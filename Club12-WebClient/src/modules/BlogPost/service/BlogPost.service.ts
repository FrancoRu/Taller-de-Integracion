import routes from "../../core/constants/envVariables";
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from "../../core/utils/utilsAxios";
import {
  AddBlogPostRequest,
  BlogPostFiltered,
  PutBlogPostRequest,
} from "../type/BlogPost";

/**
 * BlogPostService provides methods to interact with the blog posts API.
 */
export const BlogPostService = {
  /**
   * Adds a new blog post.
   * @param {AddBlogPostRequest} post - The post data to be added.
   * @returns {Promise} - A promise that resolves with the server response.
   */
  addBlogPost: async (post: AddBlogPostRequest): Promise<unknown> =>
    await sendPost(routes.blogposts, post),

  /**
   * Updates an existing blog post by its ID.
   * @param {string} id - The ID of the blog post to be updated.
   * @param {PutBlogPostRequest} post - The updated post data.
   * @returns {Promise} - A promise that resolves with the server response.
   */
  putBlogPostById: async (
    id: string,
    post: PutBlogPostRequest
  ): Promise<unknown> => await sendPut(`${routes.blogposts}/${id}`, post),

  /**
   * Updates the photo of an existing blog post by its ID.
   * @param {string} id - The ID of the blog post.
   * @param {File} photo - The new photo file to be uploaded.
   * @returns {Promise} - A promise that resolves with the server response.
   */
  putPhotoBlogPostById: async (id: string, photo: File): Promise<unknown> =>
    await sendPut(`${routes.blogposts}/${id}/photo`, photo),

  /**
   * Gets a blog post by its ID.
   * @param {string} id - The ID of the blog post to retrieve.
   * @returns {Promise} - A promise that resolves with the blog post data.
   */
  getBlogPostsById: async (id: string): Promise<unknown> =>
    await sendGet(`${routes.blogposts}/${id}`),

  /**
   * Gets a list of blog posts based on filters.
   * @param {BlogPostFiltered} filter - The filters to apply when retrieving blog posts.
   * @returns {Promise} - A promise that resolves with a list of blog posts matching the filter.
   */
  getBlogPostsByFilters: async (filter: BlogPostFiltered): Promise<unknown> =>
    await sendGet(routes.blogposts, filter),

  /**
   * Deletes a blog post by its ID.
   * @param {string} id - The ID of the blog post to delete.
   * @returns {Promise} - A promise that resolves when the blog post is deleted.
   */
  deleteBlogPostById: async (id: string): Promise<unknown> =>
    await sendDelete(`${routes.blogposts}/${id}`),
};
