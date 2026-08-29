import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  TextField,
  Button,
  FormControlLabel,
  Stack,
  Switch,
  Typography,
} from '@mui/material';
import ReactQuill from 'react-quill-new';
import 'react-quill-new/dist/quill.snow.css';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { CreateBlogPostRequest } from '@/modules/blogPost/type/blogPost';
import { notifySuccess } from '@/modules/core/utils/confirmDialog';
import PageShell from '@/views/core/components/PageShell';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import BlogPostImageField from '@/views/blogPost/BlogPostImageField';
import BlogPostPreviewDialog from '@/views/blogPost/BlogPostPreviewDialog';

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
    isPublished: true,
  });
  const [previewOpen, setPreviewOpen] = useState(false);
  const [photoObjectUrl, setPhotoObjectUrl] = useState<string | undefined>(undefined);

  useEffect(() => {
    if (!formData.photoFile) {
      setPhotoObjectUrl(undefined);
      return;
    }

    const url = URL.createObjectURL(formData.photoFile);
    setPhotoObjectUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [formData.photoFile]);

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
  };

  const handleQuillChange = (content: string) => {
    setFormData({ ...formData, markdownText: content });
  };

  const handlePhotoSelect = (file: File) => {
    setFormData(prev => ({ ...prev, photoFile: file }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    const result = await addBlogPost(formData);
    setSubmitting(false);

    if (result) {
      await notifySuccess({
        title: formData.isPublished
          ? 'La publicación fue creada y publicada exitosamente.'
          : 'La publicación fue guardada como borrador.',
      });
      navigate(APP_ROUTES.panelBlog);
    }
  };

  return (
    <PageShell title="Nueva publicación" maxWidth="sm">
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
          <BlogPostImageField
            previewUrl={photoObjectUrl}
            hasImage={Boolean(formData.photoFile)}
            onFileSelect={handlePhotoSelect}
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
          <FormControlLabel
            control={
              <Switch
                checked={formData.isPublished ?? true}
                onChange={(_, checked) =>
                  setFormData(prev => ({ ...prev, isPublished: checked }))
                }
              />
            }
            label={
              formData.isPublished
                ? 'Publicada (visible en el sitio)'
                : 'Borrador (no visible al público)'
            }
          />
          <Stack direction="row" spacing={2} sx={{ mt: 3, justifyContent: 'space-between' }}>
            <Button type="button" variant="outlined" onClick={() => setPreviewOpen(true)}>
              Vista previa
            </Button>
            <Button
              type="submit"
              variant="contained"
              color="primary"
              disabled={submitting}
            >
              {submitting
                ? 'Guardando...'
                : formData.isPublished
                  ? 'Publicar'
                  : 'Guardar borrador'}
            </Button>
          </Stack>
        </form>

      <BlogPostPreviewDialog
        open={previewOpen}
        onClose={() => setPreviewOpen(false)}
        title={formData.title}
        author={formData.author}
        photoUrl={photoObjectUrl}
        markdownText={formData.markdownText}
      />
    </PageShell>
  );
};

export default AddBlogPostForm;
