import { Box, Typography } from "@mui/material";
import { BlogPostProvider } from "../../modules/blogPost/context/blogPost.context";
import AddBlogPostForm from "../blogPost/addBlogPostForm"; // Import AddBlogPostForm

const Dashboard = () => {
  return (
    <Box sx={{ padding: 4 }}>
      <Typography variant="h3">Welcome to the Admin Dashboard</Typography>
      <Typography variant="body1" sx={{ marginTop: 2 }}>
        Here you can manage blog posts, user settings, and more!
      </Typography>

      <BlogPostProvider>
        <AddBlogPostForm />
      </BlogPostProvider>
    </Box>
  );
};

export default Dashboard;
