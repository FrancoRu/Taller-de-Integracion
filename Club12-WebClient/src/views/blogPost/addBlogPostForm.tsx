import React, { useState } from "react";
import { TextField, Button, Card, CardContent, Typography } from "@mui/material";
import { useBlogPost } from "../../modules/blogPost/hook/blogPost.hook";
import { CreateBlogPostRequest } from "../../modules/blogPost/type/blogPost";

/**
 * AddBlogPostForm handles the blog post creation.
 */
const AddBlogPostForm: React.FC = () => {
  const { addBlogPost } = useBlogPost();
  const [formData, setFormData] = useState<CreateBlogPostRequest>({
    author: "",
    title: "",
    markdownText: "",
  });

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    // Submit form data to add blog post
    addBlogPost(formData);
  };

  return (
    <Card sx={{ maxWidth: 600, margin: "auto", padding: 3 }}>
      <CardContent>
        <Typography variant="h5" gutterBottom>
          Add New Blog Post
        </Typography>
        <form onSubmit={handleSubmit}>
          <TextField
            label="Author"
            name="author"
            value={formData.author}
            onChange={handleInputChange}
            fullWidth
            required
            margin="normal"
          />
          <TextField
            label="Title"
            name="title"
            value={formData.title}
            onChange={handleInputChange}
            fullWidth
            required
            margin="normal"
          />
          <TextField
            label="Markdown Text"
            name="markdownText"
            value={formData.markdownText}
            onChange={handleInputChange}
            fullWidth
            multiline
            rows={4}
            required
            margin="normal"
          />
          <Button type="submit" variant="contained" color="primary">
            Submit
          </Button>
        </form>
      </CardContent>
    </Card>
  );
};

export default AddBlogPostForm;
