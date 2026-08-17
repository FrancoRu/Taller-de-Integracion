import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  TextField,
  Button,
  Card,
  CardContent,
  Typography,
} from '@mui/material';
import ReactQuill from 'react-quill-new';
import 'react-quill-new/dist/quill.snow.css';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { CreateBlogPostRequest } from '@/modules/blogPost/type/blogPost';
import { notifySuccess } from '@/modules/core/utils/confirmDialog';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

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
  const navigate = useNavigate();
  const [submitting, setSubmitting] = useState(false);
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
    setSubmitting(true);
    const result = await addBlogPost(formData);
    setSubmitting(false);

    if (result) {
      await notifySuccess({ title: 'La publicación fue creada exitosamente.' });
      navigate(APP_ROUTES.panelBlog);
    }
  };

  return (
    <Card sx={{ maxWidth: 600, margin: 'auto', padding: 3 }}>
      <CardContent>
        <Typography variant="h5" component="h1" gutterBottom>
          Nueva publicación
        </Typography>
        <form onSubmit={handleSubmit}>
          <TextField
            label="Autor"
            name="author"
            value={formData.author}
            onChange={handleInputChange}
            fullWidth
            required
            margin="normal"
          />
          <TextField
            label="Título"
            name="title"
            value={formData.title}
            onChange={handleInputChange}
            fullWidth
            required
            margin="normal"
          />
          <Typography variant="subtitle1" component="p" sx={{ mt: 2, mb: 1 }}>
            Contenido
          </Typography>
          <ReactQuill
            theme="snow"
            value={formData.markdownText}
            onChange={handleQuillChange}
            modules={quillModules}
            style={{ height: '200px', marginBottom: '20px' }}
          />
          <Button
            type="submit"
            variant="contained"
            color="primary"
            disabled={submitting}
            sx={{ mt: 3 }}
          >
            {submitting ? 'Publicando...' : 'Publicar'}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
};

export default AddBlogPostForm;
