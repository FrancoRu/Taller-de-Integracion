import { useAuth } from '../../modules/auth/hook/auth.hook';
import { BlogPostProvider } from '../../modules/blogPost/context/blogPost.context';
import AddBlogPostForm from '../blogPost/addBlogPostForm';
import ShowPosts from '../blogPost/showPosts';

export default function Home() {
  const { isAuthenticated } = useAuth();
  return (
    <>
      <BlogPostProvider>
        {isAuthenticated ? <AddBlogPostForm /> : <ShowPosts />}
      </BlogPostProvider>
    </>
  );
}
