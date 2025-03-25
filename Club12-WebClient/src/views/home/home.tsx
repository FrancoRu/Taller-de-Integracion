import { BlogPostProvider } from '../../modules/blogPost/context/blogPost.context'
import AddBlogPostForm from '../blogPost/addBlogPostForm'

export default function Home(){
  
  return (
    <>
      <BlogPostProvider>
        <AddBlogPostForm />
      </BlogPostProvider>
    </>
  )
}
