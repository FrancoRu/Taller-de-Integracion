import { AxiosError, AxiosResponse } from 'axios';
import React, { createContext, ReactNode, useCallback, useMemo } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  FetchOptions,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { blogPostService } from '@/modules/blogPost/service/blogPost.service';
import {
  BlogPostResponse,
  CreateBlogPostRequest,
  GetBlogPostsFilteredRequest,
  IBlogPostContextProps,
  UpdateBlogPostRequest,
} from '@/modules/blogPost/type/blogPost';
import { blogPostKeys } from '@/modules/blogPost/queryKeys';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

export const BlogPostContext = createContext<IBlogPostContextProps | undefined>(
  undefined
);

export const BlogPostProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { setMessage } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

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
            blogPostKeys.byId(response.data.id),
            response
          );
          await queryClient.invalidateQueries({
            queryKey: blogPostKeys.list(),
          });
          return response.data;
        }

        throw new AxiosError(
          'Respuesta del servidor inválida.',
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

  /**
   * Updates a blog post by id. When the response is 204 No Content there is
   * no body to cache — the invalidation below is enough to refresh callers.
   */
  const putBlogPostById = useCallback(
    async (
      id: GUID,
      post: UpdateBlogPostRequest
    ): Promise<BlogPostResponse | void> => {
      try {
        const response = await putBlogPostMutation.mutateAsync({ id, post });
        if (response) {
          if (response.status === HttpStatus.NoContent) {
            // no-op
          } else if (response.data) {
            queryClient.setQueryData(blogPostKeys.byId(id), response);
            return response.data;
          }
          await queryClient.invalidateQueries({
            queryKey: blogPostKeys.list(),
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
          queryKey: blogPostKeys.byId(id),
        });
        await queryClient.invalidateQueries({ queryKey: blogPostKeys.list() });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putPhotoBlogPostMutation, queryClient, handleUnknownError]
  );

  const getBlogPostsById = useCallback(
    async (
      idOrSlug: string,
      options?: FetchOptions
    ): Promise<BlogPostResponse | void> => {
      try {
        const response = await queryClient.fetchQuery({
          queryKey: blogPostKeys.byId(idOrSlug),
          queryFn: async () => await blogPostService.getBlogPostsById(idOrSlug),
        });

        return response?.data;
      } catch (error: unknown) {
        if (!options?.silent) handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const getBlogPostsByFilters = useCallback(
    async (
      filter: GetBlogPostsFilteredRequest,
      options?: FetchOptions
    ): Promise<GenericResponsePagination<BlogPostResponse> | void> => {
      try {
        const response = await queryClient.fetchQuery({
          queryKey: blogPostKeys.list(filter),
          queryFn: async () =>
            await blogPostService.getBlogPostsByFilters(filter),
        });

        return response?.data;
      } catch (error: unknown) {
        if (!options?.silent) handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const deleteBlogPostById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await deleteBlogPostMutation.mutateAsync(id);
        queryClient.removeQueries({ queryKey: blogPostKeys.byId(id) });
        await queryClient.invalidateQueries({ queryKey: blogPostKeys.list() });
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
