import React, { useState } from 'react';
import {
  TextField,
  Button,
  Card,
  CardContent,
  Typography,
} from '@mui/material';
import ReactQuill from 'react-quill';
import 'react-quill/dist/quill.snow.css';
import { useBlogPost } from '../../modules/blogPost/hook/blogPost.hook';
import { CreateBlogPostRequest } from '../../modules/blogPost/type/blogPost';

const quillModules = {
  toolbar: {
    container: [
      [{ header: [1, 2, 3, false] }],
      ['bold', 'italic', 'underline', 'strike'],
      ['blockquote', 'code-block'],
      [{ list: 'ordered' }, { list: 'bullet' }],
      [{ script: 'sub' }, { script: 'super' }],
      [{ indent: '-1' }, { indent: '+1' }],
      [{ direction: 'rtl' }],
      [{ size: ['small', false, 'large', 'huge'] }],
      [{ color: [] }, { background: [] }],
      [{ font: [] }],
      [{ align: [] }],
      ['link', 'image', 'video'],
      ['clean'],
    ],
  },
};

const AddBlogPostForm: React.FC = () => {
  const { addBlogPost } = useBlogPost();
  const [formData, setFormData] = useState<CreateBlogPostRequest>({
    author: '',
    title: '',
    markdownText: '',
  });

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
  };

  const handleQuillChange = (content: string) => {
    setFormData({ ...formData, markdownText: content });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    addBlogPost(formData);
  };

  return (
    <Card sx={{ maxWidth: 600, margin: 'auto', padding: 3 }}>
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
          <Typography variant="subtitle1" sx={{ mt: 2, mb: 1 }}>
            Blog Content (HTML)
          </Typography>
          <ReactQuill
            theme="snow"
            value={formData.markdownText}
            onChange={handleQuillChange}
            modules={quillModules}
            style={{ height: '200px', marginBottom: '20px' }}
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
