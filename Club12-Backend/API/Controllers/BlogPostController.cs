using AutoMapper;
using API.Utils;
using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;
using Application.DTOs.BlogPosts.Response;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Domain.Entities.Models;

namespace API.Controllers;


/// <summary>
/// Controller for managing blog posts.
/// </summary>
/// <param name="_blogPostService">The blog post service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
//[Authorize(Roles = "SuperAdmin")]
[Route("api/blogposts/")]
[ApiController]
public class BlogPostController(
    IBlogPostService _blogPostService,
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

        }

        BlogPost blogPost = _mapper.Map<BlogPost>(blogPostRequest);
        blogPost.PhotoUrl = photoUrl;

        BlogPost createdBlogPost = await _blogPostService.CreateBlogPostAsync(blogPost);
        BlogPostResponse blogPostResponse = _mapper.Map<BlogPostResponse>(createdBlogPost);

        return new ObjectResult(blogPostResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Updates an existing blog post by its id.
    /// </summary>
    /// <param name="id">The id of the blog post to update.</param>
    /// <param name="blogPostRequest">The blog post request with updated content.</param>
    /// <returns>Returns 200 (OK) with the updated blog post response if the update was successful, or 400 (Bad Request) if the blog post was not found.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BlogPostResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateBlogPost(Guid id, UpdateBlogPostRequest blogPostRequest)
    {
        BlogPost? existingPost = await _blogPostService.GetBlogPostByIdAsync(id);

        if (existingPost is null)
        {
            return BadRequest($"Blog post with id {id} not found.");
        }

        _mapper.Map(blogPostRequest, existingPost);
        await _blogPostService.UpdateBlogPostAsync(existingPost);

        return Ok();
    }

    /// <summary>
    /// Updates the photo of a blog post.
    /// </summary>
    /// <param name="id">The id of the blog post to update the photo.</param>
    /// <param name="photoRequest">The update blog post photo request.</param>
    /// <returns>Returns 200 (OK) if the photo was successfully updated.</returns>
    [HttpPut("{id:guid}/photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateBlogPostPhoto(Guid id, UpdateBlogPostPhotoRequest photoRequest)
    {
        if (!photoRequest.PhotoFile.IsValidImageFile())
        {
            return BadRequest("The photo file must be a valid JPEG/PNG image.");
        }

        BlogPost? blogPost = await _blogPostService.GetBlogPostByIdAsync(id);
        if (blogPost is null)
        {
            return BadRequest($"Blog post with id {id} not found.");
        }

        //string photoUrl = await _cloudflareService.UploadFileAsync(photoRequest.PhotoFile.OpenReadStream(), photoRequest.PhotoFile.FileName);
        //blogPost.PhotoUrl = photoUrl;

        await _blogPostService.UpdateBlogPostAsync(blogPost);
        return Ok();
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
        BlogPost? blogPost = await _blogPostService.GetBlogPostByIdAsync(id);

        if (blogPost is null)
        {
            return BadRequest($"Blog post with id {id} not found.");
        }

        blogPost.Views++;
        await _blogPostService.UpdateBlogPostAsync(blogPost);


        BlogPostResponse blogPostResponse = _mapper.Map<BlogPostResponse>(blogPost);
        return Ok(blogPostResponse);
    }

    /// <summary>
    /// Deletes a blog post by its id.
    /// </summary>
    /// <param name="id">The id of the blog post to delete.</param>
    /// <returns>Returns 204 (No Content) if the blog post was successfully deleted, or 400 (Bad Request) if not found.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteBlogPostById(Guid id)
    {
        await _blogPostService.DeleteBlogPostAsync(id);
        return NoContent();
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