import { GenericResponsePagination } from "../../core/types/types";

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
    id: string,
    post: UpdateBlogPostRequest
  ): Promise<BlogPostResponse | void>;

  /**
   * Updates the photo of an existing blog post by its ID.
   * @param id The ID of the blog post to update the photo for.
   * @param photo The new photo file to upload.
   * @returns A promise that resolves when the photo is successfully updated.
   */
  putPhotoBlogPostById(id: string, photo: IFormFile): Promise<void>;

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
    filter: GetBlogPostsFilteredRequest
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
   * @type {IFormFile}
   */
  photoFile?: IFormFile;

  /**
   * The markdown text content of the blog post.
   * @type {string}
   */
  markdownText: string;
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
}

/**
 * The request body structure for updating the photo of an existing blog post.
 * @interface UpdateBlogPostPhotoRequest
 */
export interface UpdateBlogPostPhotoRequest {
  /**
   * The file of the blog post photo to be updated.
   * @type {IFormFile}
   */
  photoFile: IFormFile;
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
}

/**
 * The request body structure for fetching filtered blog posts with pagination.
 * @interface GetBlogPostsFilteredRequest
 */
export interface GetBlogPostsFilteredRequest {
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

  /**
   * A keyword to search within the markdown text content of the blog post.
   * @type {string}
   */
  keyword?: string;

  /**
   * Pagination properties from `PaginatedFilterRequest`.
   * @type {number}
   */
  pageIndex: number;

  /**
   * Pagination properties from `PaginatedFilterRequest`.
   * @type {number}
   */
  pageSize: number;
}
