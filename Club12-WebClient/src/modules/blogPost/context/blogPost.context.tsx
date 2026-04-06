import { AxiosError, AxiosResponse } from 'axios';
import React, { createContext, ReactNode, useCallback, useMemo } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { blogPostService } from '@/modules/blogPost/service/blogPost.service';
import {
  BlogPostResponse,
  CreateBlogPostRequest,
  GetBlogPostsFilteredRequest,
  IBlogPostContextProps,
  UpdateBlogPostRequest,
} from '@/modules/blogPost/type/blogPost';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';

export const BlogPostContext = createContext<IBlogPostContextProps | undefined>(
  undefined
);

export const BlogPostProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { setError, setMessage } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = useCallback(
    (error: unknown) => {
      if (error instanceof AxiosError) {
        setError(error);
        return;
      }

      setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
    },
    [setError]
  );

  const addBlogPostMutation = useMutation({
    mutationFn: blogPostService.addBlogPost,
  });

  const putBlogPostMutation = useMutation({
    mutationFn: ({ id, post }: { id: GUID; post: UpdateBlogPostRequest }) =>
      blogPostService.putBlogPostById(id, post),
  });

  const putPhotoBlogPostMutation = useMutation({
    mutationFn: ({ id, photo }: { id: GUID; photo: File }) =>
      blogPostService.putPhotoBlogPostById(id, photo),
  });

  const deleteBlogPostMutation = useMutation({
    mutationFn: blogPostService.deleteBlogPostById,
  });

  const addBlogPost = useCallback(
    async (post: CreateBlogPostRequest): Promise<BlogPostResponse | void> => {
      try {
        const response: AxiosResponse<BlogPostResponse> =
          await addBlogPostMutation.mutateAsync(post);

        if (response && response.data) {
          setMessage(response.status, ['Blog Post created successfully']);
          queryClient.setQueryData(
            ['blogPost', 'byId', response.data.id],
            response
          );
          await queryClient.invalidateQueries({
            queryKey: ['blogPost', 'list'],
          });
          return response.data;
        }

        throw new AxiosError(
          'Invalid response data',
          undefined,
          undefined,
          response
        );
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addBlogPostMutation, setMessage, queryClient, handleUnknownError]
  );

  const putBlogPostById = useCallback(
    async (
      id: GUID,
      post: UpdateBlogPostRequest
    ): Promise<BlogPostResponse | void> => {
      try {
        const response = await putBlogPostMutation.mutateAsync({ id, post });
        if (response) {
          if (response.status === 204) {
            // NoContent: no returned body, cache invalidation is enough
          } else if (response.data) {
            queryClient.setQueryData(['blogPost', 'byId', id], response);
            return response.data;
          }
          await queryClient.invalidateQueries({
            queryKey: ['blogPost', 'list'],
          });
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putBlogPostMutation, queryClient, handleUnknownError]
  );

  const putPhotoBlogPostById = useCallback(
    async (id: GUID, photo: File): Promise<void> => {
      try {
        await putPhotoBlogPostMutation.mutateAsync({ id, photo });
        await queryClient.invalidateQueries({
          queryKey: ['blogPost', 'byId', id],
        });
        await queryClient.invalidateQueries({ queryKey: ['blogPost', 'list'] });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putPhotoBlogPostMutation, queryClient, handleUnknownError]
  );

  const getBlogPostsById = useCallback(
    async (id: GUID): Promise<BlogPostResponse | void> => {
      try {
        const response = await queryClient.fetchQuery({
          queryKey: ['blogPost', 'byId', id],
          queryFn: async () => await blogPostService.getBlogPostsById(id),
        });

        return response?.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const getBlogPostsByFilters = useCallback(
    async (
      filter: GetBlogPostsFilteredRequest
    ): Promise<GenericResponsePagination<BlogPostResponse> | void> => {
      try {
        const response = await queryClient.fetchQuery({
          queryKey: ['blogPost', 'list', filter],
          queryFn: async () =>
            await blogPostService.getBlogPostsByFilters(filter),
        });

        return response?.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const deleteBlogPostById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await deleteBlogPostMutation.mutateAsync(id);
        queryClient.removeQueries({ queryKey: ['blogPost', 'byId', id] });
        await queryClient.invalidateQueries({ queryKey: ['blogPost', 'list'] });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [deleteBlogPostMutation, queryClient, handleUnknownError]
  );

  const container: IBlogPostContextProps = useMemo(
    () => ({
      addBlogPost,
      putBlogPostById,
      putPhotoBlogPostById,
      getBlogPostsById,
      getBlogPostsByFilters,
      deleteBlogPostById,
    }),
    [
      addBlogPost,
      putBlogPostById,
      putPhotoBlogPostById,
      getBlogPostsById,
      getBlogPostsByFilters,
      deleteBlogPostById,
    ]
  );
  return (
    <BlogPostContext.Provider value={container}>
      {children}
    </BlogPostContext.Provider>
  );
};
