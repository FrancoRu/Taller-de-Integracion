import {
  FetchOptions,
  Filtered,
  GenericResponsePagination,
} from '@/modules/core/types/types';

/**
 * Context properties and methods for managing blog posts in a React application.
 * These methods interact with the backend for creating, updating, fetching, and deleting blog posts.
 * @interface IBlogPostContextProps
 */
export interface IBlogPostContextProps {
  /**
   * Adds a new blog post.
   * @param post The details of the blog post to add.
   * @returns A promise that resolves with the response containing the newly added blog post.
   */
  addBlogPost(post: CreateBlogPostRequest): Promise<BlogPostResponse | void>;

  /**
   * Updates an existing blog post by its ID.
   * @param id The ID of the blog post to update.
   * @param post The updated blog post data.
   * @returns A promise that resolves with the response containing the updated blog post.
   */
  putBlogPostById(
    id: GUID,
    post: UpdateBlogPostRequest
  ): Promise<BlogPostResponse | void>;

  /**
   * Updates the photo of an existing blog post by its ID.
   * @param id The ID of the blog post to update the photo for.
   * @param photo The new photo file to upload.
   * @returns A promise that resolves when the photo is successfully updated.
   */
  putPhotoBlogPostById(id: GUID, photo: File): Promise<BlogPostResponse | void>;

  /**
   * Fetches a blog post by its ID or its public slug.
   * @param idOrSlug The ID or slug of the blog post to fetch.
   * @returns A promise that resolves with the blog post data.
   */
  getBlogPostsById(
    idOrSlug: string,
    options?: FetchOptions
  ): Promise<BlogPostResponse | void>;

  /**
   * Fetches blog posts based on filters and pagination.
   * @param filter The filter criteria to apply when fetching blog posts.
   * @param options Per-call options; `silent` suppresses the global alert on failure.
   * @returns A promise that resolves with a paginated response containing filtered blog posts.
   */
  getBlogPostsByFilters(
    filter: GetBlogPostsFilteredRequest,
    options?: FetchOptions
  ): Promise<GenericResponsePagination<BlogPostResponse> | void>;

  /**
   * Deletes a blog post by its ID.
   * @param id The ID of the blog post to delete.
   * @returns A promise resolving to `true` if the blog post was deleted,
   * `false` if the request failed (the global error is already reported
   * either way).
   */
  deleteBlogPostById(id: GUID): Promise<boolean>;
}

/**
 * The request body structure for adding a new blog post.
 * @interface CreateBlogPostRequest
 */
export interface CreateBlogPostRequest {
  /**
   * The author of the blog post.
   * @type {string}
   */
  author: string;

  /**
   * The title of the blog post.
   * @type {string}
   */
  title: string;

  /**
   * The photo file to upload for the blog post (optional).
   * @type {File}
   */
  photoFile?: File;

  /**
   * The markdown text content of the blog post.
   * @type {string}
   */
  markdownText: string;

  /**
   * Whether the post is published (visible publicly) or saved as a draft
   * (HU-16). Defaults to published when omitted.
   * @type {boolean}
   */
  isPublished?: boolean;
}

/**
 * The request body structure for updating an existing blog post.
 * @interface UpdateBlogPostRequest
 */
export interface UpdateBlogPostRequest {
  /**
   * The updated title of the blog post.
   * @type {string}
   */
  title?: string;

  /**
   * The updated markdown text content of the blog post.
   * @type {string}
   */
  markdownText?: string;

  /**
   * The updated author of the blog post.
   * @type {string}
   */
  author?: string;

  /**
   * The updated publication state (HU-16). `undefined` leaves the current
   * state untouched; `true` publishes, `false` turns it back into a draft.
   * @type {boolean}
   */
  isPublished?: boolean;
}

/**
 * The request body structure for updating the photo of an existing blog post.
 * @interface UpdateBlogPostPhotoRequest
 */
export interface UpdateBlogPostPhotoRequest {
  /**
   * The file of the blog post photo to be updated.
   * @type {File}
   */
  photoFile: File;
}

/**
 * The response structure for a blog post, including its details and metadata.
 * @interface BlogPostResponse
 */
export interface BlogPostResponse {
  /**
   * The unique identifier of the blog post.
   * @type {string}
   */
  id: GUID;

  /**
   * The author of the blog post.
   * @type {string}
   */
  author: string;

  /**
   * The title of the blog post.
   * @type {string}
   */
  title: string;

  /**
   * The unique, URL-friendly identifier used in public blog post links.
   * @type {string}
   */
  slug: string;

  /**
   * The number of views the blog post has received.
   * @type {number}
   */
  views: number;

  /**
   * The URL of the photo associated with the blog post.
   * @type {string}
   */
  photoUrl?: string;

  /**
   * The markdown text content of the blog post.
   * @type {string}
   */
  markdownText: string;

  /**
   * The date and time the blog post was created.
   * @type {Date}
   */
  createdAt: Date;

  /**
   * Whether the post is published (visible publicly) or a draft (HU-16).
   * @type {boolean}
   */
  isPublished: boolean;
}

/**
 * The request body structure for fetching filtered blog posts with pagination.
 * @interface GetBlogPostsFilteredRequest
 */
export interface GetBlogPostsFilteredRequest extends Filtered {
  /**
   * The author to filter blog posts by.
   * @type {string}
   */
  author?: string;

  /**
   * The title to filter blog posts by.
   * @type {string}
   */
  title?: string;
}
