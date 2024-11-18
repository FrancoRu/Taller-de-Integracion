import { Filetered, GenericResponsePagination } from "../../core/types/types";

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
  addBlogPost(post: AddBlogPostRequest): Promise<BlogPostResponse | void>;

  /**
   * Updates an existing blog post by its ID.
   * @param id The ID of the blog post to update.
   * @param post The updated blog post data.
   * @returns A promise that resolves with the response containing the updated blog post.
   */
  putBlogPostById(
    id: string,
    post: PutBlogPostRequest
  ): Promise<BlogPostResponse | void>;

  /**
   * Updates the photo of an existing blog post by its ID.
   * @param id The ID of the blog post to update the photo for.
   * @param photo The new photo file to upload.
   * @returns A promise that resolves when the photo is successfully updated.
   */
  putPhotoBlogPostById(id: string, photo: File): Promise<void>;

  /**
   * Fetches a blog post by its ID.
   * @param id The ID of the blog post to fetch.
   * @returns A promise that resolves with the blog post data.
   */
  getBlogPostsById(id: string): Promise<BlogPostResponse | void>;

  /**
   * Fetches blog posts based on filters and pagination.
   * @param filter The filter criteria to apply when fetching blog posts.
   * @returns A promise that resolves with a paginated response containing filtered blog posts.
   */
  getBlogPostsByFilters(
    filter: BlogPostFiltered
  ): Promise<GenericResponsePagination<BlogPostResponse> | void>;

  /**
   * Deletes a blog post by its ID.
   * @param id The ID of the blog post to delete.
   * @returns A promise that resolves when the blog post is successfully deleted.
   */
  deleteBlogPostById(id: string): Promise<void>;
}

/**
 * The request body structure for adding a new blog post.
 * @interface AddBlogPostRequest
 */
export interface AddBlogPostRequest {
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
   * The photo file of the blog post (optional).
   * @type {File}
   */
  photoFile?: File;

  /**
   * The markdown text content of the blog post.
   * @type {string}
   */
  markdownText: string;
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
  id: string;

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
   * The number of views the blog post has received.
   * @type {number}
   */
  views: number;

  /**
   * The URL of the blog post's photo.
   * @type {string}
   */
  photoUrl: string;

  /**
   * The markdown text content of the blog post.
   * @type {string}
   */
  markdownText: string;

  /**
   * The date when the blog post was created.
   * @type {Date}
   */
  createdAt: Date;
}

/**
 * The filter criteria for fetching blog posts.
 * This extends from Filetered and includes additional properties for filtering by title, author, and keyword.
 * @interface BlogPostFiltered
 * @extends Filetered
 */
export interface BlogPostFiltered extends Filetered {
  /**
   * The title of the blog post to filter by.
   * @type {string}
   */
  title?: string;

  /**
   * The author of the blog post to filter by.
   * @type {string}
   */
  author?: string;

  /**
   * The keyword to filter blog posts by.
   * @type {string}
   */
  keyword?: string;
}

/**
 * The request body structure for updating an existing blog post.
 * @interface PutBlogPostRequest
 */
export interface PutBlogPostRequest {
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
}
