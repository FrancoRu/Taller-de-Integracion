import { GUID } from '@/modules/core/types/types';
import { GetBlogPostsFilteredRequest } from '@/modules/blogPost/type/blogPost';

export const blogPostKeys = {
  list: (filter?: GetBlogPostsFilteredRequest) =>
    filter === undefined
      ? (['blogPost', 'list'] as const)
      : (['blogPost', 'list', filter] as const),
  byId: (id: GUID) => ['blogPost', 'byId', id] as const,
};
