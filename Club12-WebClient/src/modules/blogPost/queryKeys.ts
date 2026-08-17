import { GetBlogPostsFilteredRequest } from '@/modules/blogPost/type/blogPost';

export const blogPostKeys = {
  list: (filter?: GetBlogPostsFilteredRequest) =>
    filter === undefined
      ? (['blogPost', 'list'] as const)
      : (['blogPost', 'list', filter] as const),
  byId: (idOrSlug: string) => ['blogPost', 'byId', idOrSlug] as const,
};
