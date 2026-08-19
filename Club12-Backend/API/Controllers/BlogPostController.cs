using AutoMapper;

using Club12.API.Utils;

using Entities.DTOs.Abstract;
using Entities.DTOs.BlogPost;
using Entities.Models.BlogPostEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.BlogPostService;
using Services.Utils.Cloudfare;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing blog posts.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
[Route("api/blogposts/")]
[ApiController]
public class BlogPostController(
    IBlogPostService _blogPostService,
    ICloudflareService _cloudflareService,
    IMapper _mapper
    ) : ControllerBase
{
    /// <summary>
    /// Creates a new blog post.
    /// </summary>
    /// <param name="blogPostRequest">The blog post creation request object containing the post details.</param>
    /// <returns>The created blog post response with details of the new blog post.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(BlogPostResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BlogPostResponse>> CreateBlogPost([FromForm] CreateBlogPostRequest blogPostRequest)
    {
        string? photoUrl = null;

        if (blogPostRequest.PhotoFile is not null)
        {
            if (!blogPostRequest.PhotoFile.IsValidImageFile())
            {
                return BadRequest("The photo file must be a valid JPEG/PNG image.");
            }

            photoUrl = await _cloudflareService.UploadFileAsync(blogPostRequest.PhotoFile.OpenReadStream(), blogPostRequest.PhotoFile.FileName);
        }

        BlogPost blogPost = _mapper.Map<BlogPost>(blogPostRequest);
        blogPost.PhotoUrl = photoUrl;

        BlogPost createdBlogPost = _blogPostService.CreateBlogPost(blogPost);
        BlogPostResponse blogPostResponse = _mapper.Map<BlogPostResponse>(createdBlogPost);

        return CreatedAtAction(nameof(GetBlogPostById), new { id = blogPostResponse.Id }, blogPostResponse);
    }

    /// <summary>
    /// Updates an existing blog post by its id.
    /// </summary>
    /// <param name="postId">The id of the blog post to update.</param>
    /// <param name="blogPostRequest">The blog post request with updated content.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated blog post response if the update was successful.
    /// Returns 400 (Bad Request) if the blog post with the provided id was not found.
    /// </returns>
    [HttpPut("{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BlogPostResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateBlogPost(Guid postId, UpdateBlogPostRequest blogPostRequest)
    {
        BlogPost? existingPost = _blogPostService.GetBlogPostById(postId);

        if (existingPost is null)
        {
            return BadRequest($"Blog post with id {postId} not found.");
        }

        _mapper.Map(blogPostRequest, existingPost);
        bool updateResult = await _blogPostService.UpdateBlogPostAsync(existingPost);

        return !updateResult ? BadRequest("Failed to update the blog post.") : Ok();
    }

    /// <summary>
    /// Updates the photo of a blog post.
    /// </summary>
    /// <param name="postId">The id of the blog post to update the photo.</param>
    /// <param name="photoRequest">The update blog post photo request.</param>
    /// <returns>Returns 200 (OK) if the photo was successfully updated.</returns>
    [HttpPut("{postId:guid}/photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateBlogPostPhoto(Guid postId, UpdateBlogPostPhotoRequest photoRequest)
    {
        if (!photoRequest.PhotoFile.IsValidImageFile())
        {
            return BadRequest("The photo file must be a valid JPEG/PNG image.");
        }

        BlogPost? blogPost = _blogPostService.GetBlogPostById(postId);
        if (blogPost is null)
        {
            return BadRequest($"Blog post with id {postId} not found.");
        }

        string photoUrl = await _cloudflareService.UploadFileAsync(photoRequest.PhotoFile.OpenReadStream(), photoRequest.PhotoFile.FileName);
        blogPost.PhotoUrl = photoUrl;

        bool updateResult = await _blogPostService.UpdateBlogPostAsync(blogPost);
        return !updateResult ? BadRequest("Failed to update the photo.") : Ok();
    }

    /// <summary>
    /// Retrieves a blog post by its id.
    /// </summary>
    /// <param name="id">The id of the blog post to retrieve.</param>
    /// <returns>The blog post with the specified id.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BlogPostResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BlogPostResponse>> GetBlogPostById(Guid id)
    {
        BlogPost? blogPost = _blogPostService.GetBlogPostById(id);

        if (blogPost is null)
        {
            return BadRequest($"Blog post with id {id} not found.");
        }

        blogPost.Views++;

        bool updateSuccess = await _blogPostService.UpdateBlogPostAsync(blogPost);
        if (!updateSuccess)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Sorry, we ran into an issue, please try again later.");
        }

        BlogPostResponse blogPostResponse = _mapper.Map<BlogPostResponse>(blogPost);
        return Ok(blogPostResponse);
    }

    /// <summary>
    /// Deletes a blog post by its id.
    /// </summary>
    /// <param name="id">The id of the blog post to delete.</param>
    /// <returns>Returns 200 (OK) if the blog post was successfully deleted.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult DeleteBlogPostById(Guid id)
    {
        BlogPost? blogPost = _blogPostService.GetBlogPostById(id);

        if (blogPost is null)
        {
            return BadRequest($"Blog post with id {id} not found.");
        }

        _blogPostService.DeleteBlogPost(blogPost);
        return Ok();
    }

    /// <summary>
    /// Retrieves filtered blog posts with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered blog posts.</returns>
    [AllowAnonymous]
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<BlogPostResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<BlogPostResponse>>> GetFilteredBlogPosts([FromQuery] GetBlogPostsFilteredRequest filterRequest)
    {
        PaginatedResponse<BlogPost> paginatedPosts = await _blogPostService.GetAllBlogPostsAsync(filterRequest);
        PaginatedResponse<BlogPostResponse> response = _mapper.Map<PaginatedResponse<BlogPostResponse>>(paginatedPosts);

        return Ok(response);
    }
}
