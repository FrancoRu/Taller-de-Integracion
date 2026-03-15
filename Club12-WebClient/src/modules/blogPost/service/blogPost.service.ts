import { AxiosResponse } from 'axios';
import routes from '../../core/constants/routes';
import { GenericResponsePagination, GUID } from '../../core/types/types';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '../../core/utils/axiosUtils';
import {
  BlogPostResponse,
  CreateBlogPostRequest,
  GetBlogPostsFilteredRequest,
  UpdateBlogPostRequest,
} from '../type/blogPost';

/**
 * BlogPostService provides methods to interact with the blog posts API.
 */
export const blogPostService = {
  /**
   * Adds a new blog post.
   * @param {CreateBlogPostRequest} post - The post data to be added.
   * @returns {Promise<AxiosResponse<BlogPostResponse>>} - A promise that resolves with the server response.
   */
  addBlogPost: (
    post: CreateBlogPostRequest
  ): Promise<AxiosResponse<BlogPostResponse>> => {
    const formData = new FormData();
    formData.append('Author', post.author);
    formData.append('Title', post.title);
    formData.append('MarkdownText', post.markdownText);

    if (post.photoFile) {
      formData.append('PhotoFile', post.photoFile as Blob);
    }

    return sendPost<BlogPostResponse>(routes.blogposts, formData);
  },

  /**
   * Updates an existing blog post by its ID.
   * @param {string} id - The ID of the blog post to be updated.
   * @param {UpdateBlogPostRequest} post - The updated post data.
   * @returns {Promise<AxiosResponse<BlogPostResponse>>} - A promise that resolves with the server response.
   */
  putBlogPostById: async (
    id: GUID,
    post: UpdateBlogPostRequest
  ): Promise<AxiosResponse<BlogPostResponse>> => {
    const formData = new FormData();
    if (post.author) formData.append('Author', post.author);

    if (post.title) formData.append('Title', post.title);

    if (post.markdownText) formData.append('MarkdownText', post.markdownText);

    return sendPut<BlogPostResponse>(`${routes.blogposts}/${id}`, formData);
  },

  /**
   * Updates the photo of an existing blog post by its ID.
   * @param {string} id - The ID of the blog post.
   * @param {File} photo - The new photo file to be uploaded.
   * @returns {Promise<AxiosResponse<void>>} - A promise that resolves with the server response.
   */
  putPhotoBlogPostById: async (
    id: GUID,
    photo: File
  ): Promise<AxiosResponse<void>> => {
    const formData = new FormData();
    formData.append('PhotoFile', photo); // Add photo as FormData

    return sendPut<void>(`${routes.blogposts}/${id}/photo`, formData);
  },

  /**
   * Gets a blog post by its ID.
   * @param {string} id - The ID of the blog post to retrieve.
   * @returns {Promise<AxiosResponse<BlogPostResponse>>} - A promise that resolves with the blog post data.
   */
  getBlogPostsById: async (
    id: GUID
  ): Promise<AxiosResponse<BlogPostResponse>> =>
    sendGet<BlogPostResponse>(`${routes.blogposts}/${id}`),

  /**
   * Fetches blog posts based on filters and pagination.
   * @param filter The filter criteria to apply when fetching blog posts.
   * @returns A promise that resolves with a paginated response containing filtered blog posts.
   */
  getBlogPostsByFilters: async (
    filter: GetBlogPostsFilteredRequest
  ): Promise<AxiosResponse<GenericResponsePagination<BlogPostResponse>>> =>
    sendGet<GenericResponsePagination<BlogPostResponse>>(
      routes.blogposts,
      filter
    ),

  /**
   * Deletes a blog post by its ID.
   * @param {string} id - The ID of the blog post to delete.
   * @returns {Promise<AxiosResponse<void>>} - A promise that resolves when the blog post is deleted.
   */
  deleteBlogPostById: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.blogposts}/${id}`),
};
