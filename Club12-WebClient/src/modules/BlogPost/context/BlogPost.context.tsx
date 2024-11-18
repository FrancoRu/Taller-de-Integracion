import { AxiosError, AxiosResponse } from "axios";
import { createContext, ReactNode } from "react";
import { GenericResponsePagination } from "../../core/types/types";
import { useError } from "../../error/hooks/error.hock";
import { BlogPostService } from "../service/blogPost.service";
import {
  AddBlogPostRequest,
  BlogPostFiltered,
  BlogPostResponse,
  IBlogPostContextProps,
  PutBlogPostRequest,
} from "../type/blogPost";

export const BlogPostContext = createContext<IBlogPostContextProps | undefined>(
  undefined
);

export const BlogPostProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { setError, setMessage } = useError();

  const addBlogPost = async (
    post: AddBlogPostRequest
  ): Promise<BlogPostResponse | void> => {
    try {
      const response: AxiosResponse<BlogPostResponse> =
        await BlogPostService.addBlogPost(post);

      if (response && response.data) {
        setMessage(response.status, ["Blog Post created successfully"]);
        return response.data;
      }

      throw new AxiosError(
        "Invalid response data",
        undefined,
        undefined,
        response
      );
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const putBlogPostById = async (
    id: string,
    post: PutBlogPostRequest
  ): Promise<BlogPostResponse | void> => {
    try {
      await BlogPostService.putBlogPostById(id, post);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const putPhotoBlogPostById = async (
    id: string,
    photo: File
  ): Promise<void> => {
    try {
      await BlogPostService.putPhotoBlogPostById(id, photo);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const getBlogPostsById = async (
    id: string
  ): Promise<BlogPostResponse | void> => {
    try {
      await BlogPostService.getBlogPostsById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const getBlogPostsByFilters = async (
    filter: BlogPostFiltered
  ): Promise<GenericResponsePagination<BlogPostResponse> | void> => {
    try {
      await BlogPostService.getBlogPostsByFilters(filter);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const deleteBlogPostById = async (id: string): Promise<void> => {
    try {
      await BlogPostService.deleteBlogPostById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
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
