import { AxiosResponse } from "axios";
import routes from "../../core/constants/routes";
import { GenericResponsePagination } from "../../core/types/types";
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from "../../core/utils/utilsAxios";
import {
  AddBlogPostRequest,
  BlogPostFiltered,
  BlogPostResponse,
  PutBlogPostRequest,
} from "../type/blogPost";

/**
 * BlogPostService provides methods to interact with the blog posts API.
 */
export const blogPostService = {
  /**
   * Adds a new blog post.
   * @param {AddBlogPostRequest} post - The post data to be added.
   * @returns {Promise<AxiosResponse<BlogPostResponse>>} - A promise that resolves with the server response.
   */
  addBlogPost: async (
    post: AddBlogPostRequest
  ): Promise<AxiosResponse<BlogPostResponse>> =>
    await sendPost<BlogPostResponse>(routes.blogposts, post),

  /**
   * Updates an existing blog post by its ID.
   * @param {string} id - The ID of the blog post to be updated.
   * @param {PutBlogPostRequest} post - The updated post data.
   * @returns {Promise<AxiosResponse<BlogPostResponse>>} - A promise that resolves with the server response.
   */
  putBlogPostById: async (
    id: string,
    post: PutBlogPostRequest
  ): Promise<AxiosResponse<BlogPostResponse>> =>
    await sendPut<BlogPostResponse>(`${routes.blogposts}/${id}`, post),

  /**
   * Updates the photo of an existing blog post by its ID.
   * @param {string} id - The ID of the blog post.
   * @param {File} photo - The new photo file to be uploaded.
   * @returns {Promise<AxiosResponse<void>>} - A promise that resolves with the server response.
   */
  putPhotoBlogPostById: async (
    id: string,
    photo: File
  ): Promise<AxiosResponse<void>> =>
    await sendPut<void>(`${routes.blogposts}/${id}/photo`, photo),

  /**
   * Gets a blog post by its ID.
   * @param {string} id - The ID of the blog post to retrieve.
   * @returns {Promise<AxiosResponse<BlogPostResponse>>} - A promise that resolves with the blog post data.
   */
  getBlogPostsById: async (
    id: string
  ): Promise<AxiosResponse<BlogPostResponse>> =>
    await sendGet<BlogPostResponse>(`${routes.blogposts}/${id}`),

  /**
   * Gets a list of blog posts based on filters.
   * @param {BlogPostFiltered} filter - The filters to apply when retrieving blog posts.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<BlogPostResponse>>>} - A promise that resolves with a list of blog posts matching the filter.
   */
  getBlogPostsByFilters: async (
    filter: BlogPostFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<BlogPostResponse>>> =>
    await sendGet<GenericResponsePagination<BlogPostResponse>>(
      routes.blogposts,
      filter
    ),

  /**
   * Deletes a blog post by its ID.
   * @param {string} id - The ID of the blog post to delete.
   * @returns {Promise<AxiosResponse<void>>} - A promise that resolves when the blog post is deleted.
   */
  deleteBlogPostById: async (id: string): Promise<AxiosResponse<void>> =>
    await sendDelete<void>(`${routes.blogposts}/${id}`),
};
