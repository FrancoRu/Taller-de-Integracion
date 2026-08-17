import { describe, expect, it } from 'vitest';
import { blogPostKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';
import { GetBlogPostsFilteredRequest } from '@/modules/blogPost/type/blogPost';

describe('blogPostKeys', () => {
  const id: GUID = '11111111-1111-1111-1111-111111111111';

  it('list() returns the bare list literal with no trailing undefined', () => {
    expect(blogPostKeys.list()).toEqual(['blogPost', 'list']);
  });

  it('list(filter) returns the filtered list literal', () => {
    const filter: GetBlogPostsFilteredRequest = {
      author: 'jane',
      pageNumber: 1,
    };
    expect(blogPostKeys.list(filter)).toEqual(['blogPost', 'list', filter]);
  });

  it('byId(id) returns the by-id literal', () => {
    expect(blogPostKeys.byId(id)).toEqual(['blogPost', 'byId', id]);
  });
});
