import { useEffect } from 'react'
import { redirect } from 'react-router-dom'
import { useAuth } from '../../modules/auth/hook/auth.hook'
import { BlogPostProvider } from '../../modules/blogPost/context/blogPost.context'
import AddBlogPostForm from '../blogPost/addBlogPostForm'
import NavMenu from './navMenu'

export const Home = () => {
  const { isAuthenticated, logOut } = useAuth()

  useEffect(() => {
    redirect('/')
  }, [isAuthenticated])
  return (
    <>
      <NavMenu isAuthenticated={isAuthenticated} onLogout={logOut}/>
      <h1>{isAuthenticated ? 'Autenticado' : 'No autenticado'}</h1>
      <BlogPostProvider>
        <AddBlogPostForm />
      </BlogPostProvider>
    </>
  )
}
