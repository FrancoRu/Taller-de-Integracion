import { AxiosError, AxiosResponse } from 'axios';
import React, { createContext, ReactNode } from 'react';
import { GenericResponsePagination } from '../../core/types/types';
import { useError } from '../../error/hooks/error.hock';
import { blogPostService } from '../service/blogPost.service';
import {
  BlogPostResponse,
  CreateBlogPostRequest,
  GetBlogPostsFilteredRequest,
  IBlogPostContextProps,
  UpdateBlogPostRequest,
} from '../type/blogPost';

export const BlogPostContext = createContext<IBlogPostContextProps | undefined>(
  undefined
);

export const BlogPostProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { setError, setMessage } = useError();

  const addBlogPost = async (
    post: CreateBlogPostRequest
  ): Promise<BlogPostResponse | void> => {
    try {
      const response: AxiosResponse<BlogPostResponse> =
        await blogPostService.addBlogPost(post);

      if (response && response.data) {
        setMessage(response.status, ['Blog Post created successfully']);
        return response.data;
      }

      throw new AxiosError(
        'Invalid response data',
        undefined,
        undefined,
        response
      );
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const putBlogPostById = async (
    id: string,
    post: UpdateBlogPostRequest
  ): Promise<BlogPostResponse | void> => {
    try {
      await blogPostService.putBlogPostById(id, post);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const putPhotoBlogPostById = async (
    id: string,
    photo: File
  ): Promise<void> => {
    try {
      await blogPostService.putPhotoBlogPostById(id, photo);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getBlogPostsById = async (
    id: string
  ): Promise<BlogPostResponse | void> => {
    try {
      await blogPostService.getBlogPostsById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getBlogPostsByFilters = async (
    filter: GetBlogPostsFilteredRequest
  ): Promise<GenericResponsePagination<BlogPostResponse> | void> => {
    try {
      const response = await blogPostService.getBlogPostsByFilters(filter);
      return response?.data;
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const deleteBlogPostById = async (id: string): Promise<void> => {
    try {
      await blogPostService.deleteBlogPostById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const container: IBlogPostContextProps = {
    addBlogPost,
    putBlogPostById,
    putPhotoBlogPostById,
    getBlogPostsById,
    getBlogPostsByFilters,
    deleteBlogPostById,
  };
  return (
    <BlogPostContext.Provider value={container}>
      {children}
    </BlogPostContext.Provider>
  );
};
