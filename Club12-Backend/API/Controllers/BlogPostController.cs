using API.Utils;

using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;
using Application.DTOs.BlogPosts.Response;
using Application.Interfaces.Services;
using Application.Utils.Constants;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Storage;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading.Tasks;

namespace API.Controllers;


/// <summary>
/// Manages blog posts; reads are public while writes require Admin or Owner, with individual actions opting back out via AllowAnonymous where the whole club should be able to read.
/// </summary>
/// <param name="blogPostService">The blog post service.</param>
/// <param name="supabaseHelper">The Supabase helper for storage operations.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/blogposts/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class BlogPostController(
    IBlogPostService blogPostService,
    SupabaseHelper supabaseHelper,
    IMapper mapper
    ) : ControllerBase
{
    /// <summary>
    /// Creates a blog post from multipart form data, validating and uploading an included photo file to storage before the post is persisted.
    /// </summary>
    /// <param name="blogPostRequest">The blog post creation request object containing the post details.</param>
    /// <returns>The created blog post response with details of the new blog post.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(BlogPostResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BlogPostResponse>> CreateBlogPost([FromForm] CreateBlogPostRequest blogPostRequest)
    {
        if (blogPostRequest.PhotoFile is not null && !blogPostRequest.PhotoFile.IsValidImageFile())
        {
            return BadRequest(ErrorMessages.Media.InvalidImageFile);
        }

        string? photoUrl = blogPostRequest.PhotoFile is null
            ? null
            : await supabaseHelper.UploadImageAsync<BlogPost>(
                blogPostRequest.PhotoFile.OpenReadStream(),
                blogPostRequest.PhotoFile.FileName);

        BlogPost blogPost = mapper.Map<BlogPost>(blogPostRequest);
        blogPost.PhotoUrl = photoUrl;

        BlogPost createdBlogPost = await blogPostService.CreateBlogPostAsync(blogPost);
        BlogPostResponse blogPostResponse = mapper.Map<BlogPostResponse>(createdBlogPost);

        return new ObjectResult(blogPostResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Updates a blog post resolved by id or by its slug, the same lookup the GET endpoint uses, including unpublished drafts so a draft can be edited before it's published.
    /// </summary>
    /// <param name="idOrSlug">The GUID id or slug of the blog post to update.</param>
    /// <param name="blogPostRequest">The blog post request with updated content.</param>
    /// <returns>Returns 200 OK with the updated blog post response if the update was successful, or 400 Bad Request if the blog post was not found.</returns>
    [HttpPut("{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BlogPostResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateBlogPost(string idOrSlug, [FromForm] UpdateBlogPostRequest blogPostRequest)
    {
        // Resolve by id OR slug (like the GET) so the admin can save a post the
        // same way it was opened — by its slug — instead of forcing a GUID URL.
        // includeUnpublished: true so a draft can still be edited (HU-16).
        BlogPost? existingPost = await blogPostService.GetBlogPostByIdOrSlugAsync(idOrSlug, includeUnpublished: true);

        if (existingPost is null)
        {
            return this.NotFoundProblem(nameof(BlogPost), idOrSlug);
        }

        mapper.Map(blogPostRequest, existingPost);
        await blogPostService.UpdateBlogPostAsync(existingPost);

        BlogPostResponse blogPostResponse = mapper.Map<BlogPostResponse>(existingPost);
        return Ok(blogPostResponse);
    }

    /// <summary>
    /// Replaces a blog post's photo, validating the new file as an image and uploading it to storage before updating the post's PhotoUrl.
    /// </summary>
    /// <param name="idOrSlug">The GUID id or slug of the blog post to update the photo.</param>
    /// <param name="photoRequest">The update blog post photo request.</param>
    /// <returns>Returns 200 OK if the photo was successfully updated.</returns>
    [HttpPut("{idOrSlug}/photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateBlogPostPhoto(string idOrSlug, [FromForm] UpdateBlogPostPhotoRequest photoRequest)
    {
        if (!photoRequest.PhotoFile.IsValidImageFile())
        {
            return BadRequest(ErrorMessages.Media.InvalidImageFile);
        }

        BlogPost? blogPost = await blogPostService.GetBlogPostByIdOrSlugAsync(idOrSlug, includeUnpublished: true);
        if (blogPost is null)
        {
            return this.NotFoundProblem(nameof(BlogPost), idOrSlug);
        }

        blogPost.PhotoUrl = await supabaseHelper.UploadImageAsync<BlogPost>(
            photoRequest.PhotoFile.OpenReadStream(),
            photoRequest.PhotoFile.FileName);

        await blogPostService.UpdateBlogPostAsync(blogPost);
        return Ok();
    }

    /// <summary>
    /// Retrieves a blog post by id or slug; Admin or Owner can also resolve unpublished drafts, and a public non-draft read increments the view counter while an Admin/Owner preview does not.
    /// </summary>
    /// <param name="idOrSlug">Blog post identifier as a GUID, or slug.</param>
    /// <returns>The blog post with the specified id or slug.</returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BlogPostResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogPostResponse>> GetBlogPostById(string idOrSlug)
    {
        // Only Admin/Owner may resolve drafts (HU-16); anonymous/public
        // callers get 404 for an unpublished post.
        bool includeUnpublished = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Owner);
        BlogPost? blogPost = await blogPostService.GetBlogPostByIdOrSlugAsync(idOrSlug, includeUnpublished);

        if (blogPost is null)
        {
            return this.NotFoundProblem(nameof(BlogPost), idOrSlug);
        }

        // The view counter reflects genuine public reads only (HU): an
        // Admin/Owner opening the post to preview or edit it must not inflate
        // the count. Public callers (the same ones that cannot see drafts)
        // increment it exactly once per read.
        if (!includeUnpublished)
        {
            blogPost.Views++;
            await blogPostService.UpdateBlogPostAsync(blogPost);
        }

        BlogPostResponse blogPostResponse = mapper.Map<BlogPostResponse>(blogPost);
        return Ok(blogPostResponse);
    }

    /// <summary>
    /// Deletes a blog post by its id.
    /// </summary>
    /// <param name="id">The id of the blog post to delete.</param>
    /// <returns>Returns 204 No Content if the blog post was successfully deleted, or 400 Bad Request if not found.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteBlogPostById(Guid id)
    {
        await blogPostService.DeleteBlogPostAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Retrieves filtered, paginated blog posts; public and anonymous callers only see published posts while Admin or Owner also see drafts.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered blog posts.</returns>
    [AllowAnonymous]
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<BlogPostResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<BlogPostResponse>>> GetFilteredBlogPosts([FromQuery] GetBlogPostsFilteredRequest filterRequest)
    {
        // Public listing shows only published posts; Admin/Owner see drafts too (HU-16).
        bool includeUnpublished = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Owner);
        PaginatedResponse<BlogPost> paginatedPosts = await blogPostService.GetAllBlogPostsAsync(filterRequest, includeUnpublished);
        PaginatedResponse<BlogPostResponse> response = mapper.Map<PaginatedResponse<BlogPostResponse>>(paginatedPosts);

        return Ok(response);
    }
}