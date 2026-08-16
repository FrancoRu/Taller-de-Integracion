import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Box, Button, Card, CardContent, CircularProgress, Stack, TextField, Typography } from '@mui/material';
import ReactQuill from 'react-quill';
import 'react-quill/dist/quill.snow.css';
import { GUID } from '@/modules/core/types/types';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { UpdateBlogPostRequest } from '@/modules/blogPost/type/blogPost';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import FormButtons from '@/views/core/components/FormButtons';
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

const BlogPostEditPage: React.FC = () => {
  const { blogPostId } = useParams<{ blogPostId: GUID }>();
  const navigate = useNavigate();
  const { getBlogPostsById, putBlogPostById } = useBlogPost();

  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<UpdateBlogPostRequest>({
    author: '',
    title: '',
    markdownText: '',
  });
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!blogPostId) {
      setLoading(false);
      return;
    }

    const fetchPost = async () => {
      setLoading(true);
      const post = await getBlogPostsById(blogPostId);
      if (post) {
        setForm({
          author: post.author,
          title: post.title,
          markdownText: post.markdownText,
        });
      } else {
        setNotFound(true);
      }
      setLoading(false);
    };

    void fetchPost();
  }, [blogPostId, getBlogPostsById]);

  const handleCancel = useCallback(() => {
    navigate(APP_ROUTES.panelBlog);
  }, [navigate]);

  const handleSave = useCallback(async () => {
    if (!blogPostId) {
      return;
    }

    if (!form.title?.trim()) {
      await notifyWarning({ title: 'Campos incompletos', text: 'El título es obligatorio.' });
      return;
    }

    if (!form.author?.trim()) {
      await notifyWarning({ title: 'Campos incompletos', text: 'El autor es obligatorio.' });
      return;
    }

    setSubmitting(true);
    const updated = await putBlogPostById(blogPostId, {
      title: form.title.trim(),
      author: form.author.trim(),
      markdownText: form.markdownText,
    });
    setSubmitting(false);

    if (!updated) {
      return;
    }

    await notifySuccess({ title: 'Publicación actualizada', text: 'Los cambios se guardaron correctamente.' });
    navigate(APP_ROUTES.panelBlog);
  }, [blogPostId, form, putBlogPostById, navigate]);

  if (loading) {
    return (
      <Card>
        <CardContent>
          <Stack alignItems="center" spacing={2} py={4}>
            <CircularProgress />
            <Typography>Cargando publicación...</Typography>
          </Stack>
        </CardContent>
      </Card>
    );
  }

  if (notFound) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Publicación no encontrada</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No fue posible cargar la publicación solicitada.
          </Typography>
          <Box mt={2}>
            <Button variant="contained" onClick={handleCancel}>
              Volver
            </Button>
          </Box>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Stack spacing={2}>
          <Typography variant="h6">Editar publicación</Typography>

          <TextField
            label="Autor"
            value={form.author ?? ''}
            onChange={e => setForm(prev => ({ ...prev, author: e.target.value }))}
            required
            fullWidth
          />
          <TextField
            label="Título"
            value={form.title ?? ''}
            onChange={e => setForm(prev => ({ ...prev, title: e.target.value }))}
            required
            fullWidth
          />

          <Typography variant="subtitle1">Contenido</Typography>
          <ReactQuill
            theme="snow"
            value={form.markdownText ?? ''}
            onChange={content => setForm(prev => ({ ...prev, markdownText: content }))}
            modules={quillModules}
            style={{ height: 200, marginBottom: 40 }}
          />

          <Stack direction="row" justifyContent="flex-end">
            <FormButtons
              onCancel={handleCancel}
              onConfirm={() => void handleSave()}
              confirmLabel="Guardar"
              disabled={submitting}
            />
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
};

export default BlogPostEditPage;
