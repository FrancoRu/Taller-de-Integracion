import { useContext } from 'react';
import { BlogPostContext } from '@/modules/blogPost/context/blogPost.context';

export const useBlogPost = () => {
  const context = useContext(BlogPostContext);
  if (!context) {
    throw new Error('useBlogPost must be used within a BlogPostProvider');
  }
  return context;
};
