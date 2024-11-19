import React, { useEffect, useState } from "react";
import { Grid, Card, CardContent, Typography, Button, CircularProgress } from "@mui/material";
import { useBlogPost } from "../../modules/blogPost/hook/blogPost.hook";
import { BlogPostResponse, GetBlogPostsFilteredRequest } from "../../modules/blogPost/type/blogPost";
import { useNavigate } from "react-router-dom";

const ShowPosts: React.FC = () => {
  const { getBlogPostsByFilters, getBlogPostsById } = useBlogPost();
  const [posts, setPosts] = useState<BlogPostResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 5,
    totalCount: 0,
  });
  const navigate = useNavigate();

  const filterParams: GetBlogPostsFilteredRequest = {
    pageIndex: pagination.page,
    pageSize: pagination.pageSize,
    author: "",
    title: "",
    keyword: "",
  };

  useEffect(() => {
    const loadPosts = async () => {
      setLoading(true);
      try {
        const response = await getBlogPostsByFilters(filterParams);
        if (response) {
          const { items, page, pageSize, totalCount } = response;
          if (items.length > 0) {
            setPosts(items);
            setPagination({ page, pageSize, totalCount });
          }
        }
      } catch (error) {
        console.error("Error loading blog posts:", error);
      } finally {
        setLoading(false);
      }
    };
    loadPosts();
  }, [pagination.page]);

  const handlePageChange = (direction: 'next' | 'previous') => {
    if (loading) return;
    const { page, pageSize, totalCount } = pagination;
    let newPage = direction === 'next' ? page + 1 : page - 1;

    if (newPage < 1 || newPage > Math.ceil(totalCount / pageSize)) return;
    setPagination((prev) => ({ ...prev, page: newPage }));
  };

  const handleReadMore = async (id: string) => {
    try {
      const postDetails = await getBlogPostsById(id);
      if (postDetails) {
        navigate(`/blog/${id}`, { state: { post: postDetails } });
      }
    } catch (error) {
      console.error("Error fetching detailed post:", error);
    }
  };

  return (
    <div>
      <Grid container spacing={3} justifyContent="center">
        {posts.length > 0 ? (
          posts.map((post) => (
            <Grid item xs={12} sm={6} md={4} key={post.id}>
              <Card sx={{ maxWidth: 345 }}>
                <CardContent>
                  <Typography variant="h6">{post.title}</Typography>
                  <div
                    dangerouslySetInnerHTML={{
                      __html: post.markdownText.substring(0, 150) + "..."
                    }}
                  />
                  <Button
                    variant="outlined"
                    color="primary"
                    onClick={() => handleReadMore(post.id)}
                  >
                    Read More
                  </Button>
                </CardContent>
              </Card>
            </Grid>
          ))
        ) : (
          <Typography variant="body1" color="textSecondary">
            No posts available.
          </Typography>
        )}
      </Grid>

      <div style={{ textAlign: "center", marginTop: 20 }}>
        {loading ? (
          <CircularProgress />
        ) : (
          <>
            {pagination.page > 1 && (
              <Button
                variant="contained"
                color="secondary"
                onClick={() => handlePageChange('previous')}
                sx={{ marginTop: 3, marginRight: 2 }}
              >
                Previous
              </Button>
            )}

            {pagination.page * pagination.pageSize < pagination.totalCount && (
              <Button
                variant="contained"
                color="primary"
                onClick={() => handlePageChange('next')}
                sx={{ marginTop: 3 }}
              >
                Next
              </Button>
            )}
          </>
        )}
      </div>
    </div>
  );
};

export default ShowPosts;
