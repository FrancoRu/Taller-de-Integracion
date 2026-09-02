import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  FormControlLabel,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import ReactQuill from 'react-quill-new';
import 'react-quill-new/dist/quill.snow.css';
import { GUID } from '@/modules/core/types/types';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { UpdateBlogPostRequest } from '@/modules/blogPost/type/blogPost';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import FormButtons from '@/views/core/components/FormButtons';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
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

const BlogPostEditPage: React.FC = () => {
  const { blogPostId } = useParams<{ blogPostId: GUID }>();
  const navigate = useNavigate();
  const { getBlogPostsById, putBlogPostById, putPhotoBlogPostById } = useBlogPost();

  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<UpdateBlogPostRequest>({
    author: '',
    title: '',
    markdownText: '',
    isPublished: true,
  });
  const [photoUrl, setPhotoUrl] = useState<string | undefined>(undefined);
  const [photoFile, setPhotoFile] = useState<File | undefined>(undefined);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [notFound, setNotFound] = useState(false);
  // The URL param may be a SLUG, but the update/photo endpoints require the
  // post's GUID — so we keep the resolved id from the loaded post and save with
  // it, instead of PUTting to a slug URL (which 404s).
  const [resolvedId, setResolvedId] = useState<GUID | undefined>(undefined);

  useEffect(() => {
    if (!blogPostId) {
      setLoading(false);
      return;
    }

    const fetchPost = async () => {
      setLoading(true);
      const post = await getBlogPostsById(blogPostId);
      if (post) {
        setResolvedId(post.id);
        setForm({
          author: post.author,
          title: post.title,
          markdownText: post.markdownText,
          isPublished: post.isPublished,
        });
        setPhotoUrl(post.photoUrl);
      } else {
        setNotFound(true);
      }
      setLoading(false);
    };

    void fetchPost();
  }, [blogPostId, getBlogPostsById]);

  const [objectUrl, setObjectUrl] = useState<string | undefined>(undefined);

  useEffect(() => {
    if (!photoFile) {
      setObjectUrl(undefined);
      return;
    }

    const url = URL.createObjectURL(photoFile);
    setObjectUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [photoFile]);

  const displayedImageUrl = objectUrl ?? photoUrl;

  const handleCancel = useCallback(() => {
    navigate(-1);
  }, [navigate]);

  const handleSave = useCallback(async () => {
    if (!resolvedId) {
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
    const updated = await putBlogPostById(resolvedId, {
      title: form.title.trim(),
      author: form.author.trim(),
      markdownText: form.markdownText,
      isPublished: form.isPublished,
    });

    if (!updated) {
      setSubmitting(false);
      return;
    }

    if (photoFile) {
      await putPhotoBlogPostById(resolvedId, photoFile);
    }
    setSubmitting(false);

    await notifySuccess({ title: 'Publicación actualizada', text: 'Los cambios se guardaron correctamente.' });
    navigate(APP_ROUTES.panelBlog);
  }, [resolvedId, form, photoFile, putBlogPostById, putPhotoBlogPostById, navigate]);

  if (loading) {
    return (
      <PageShell title="Editar publicación">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (notFound) {
    return (
      <PageShell
        title="Publicación no encontrada"
        back={{ label: 'Volver', onClick: handleCancel }}
      >
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No fue posible cargar la publicación solicitada.
        </Typography>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Editar publicación"
      back={{ label: 'Volver', onClick: handleCancel }}
    >
        <Stack spacing={2}>
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

          <BlogPostImageField
            previewUrl={displayedImageUrl}
            hasImage={Boolean(displayedImageUrl)}
            onFileSelect={setPhotoFile}
          />

          <Typography variant="subtitle1">Contenido</Typography>
          <ReactQuill
            theme="snow"
            value={form.markdownText ?? ''}
            onChange={content => setForm(prev => ({ ...prev, markdownText: content }))}
            modules={quillModules}
            style={{ height: 200, marginBottom: 40 }}
          />

          <FormControlLabel
            control={
              <Switch
                checked={form.isPublished ?? true}
                onChange={(_, checked) =>
                  setForm(prev => ({ ...prev, isPublished: checked }))
                }
              />
            }
            label={
              form.isPublished
                ? 'Publicada (visible en el sitio)'
                : 'Borrador (no visible al público)'
            }
          />

          <Stack direction="row" sx={{
            justifyContent: "space-between"
          }}>
            <Button variant="outlined" onClick={() => setPreviewOpen(true)}>
              Vista previa
            </Button>
            <FormButtons
              onCancel={handleCancel}
              onConfirm={() => void handleSave()}
              confirmLabel="Guardar"
              disabled={submitting}
            />
          </Stack>
        </Stack>

      <BlogPostPreviewDialog
        open={previewOpen}
        onClose={() => setPreviewOpen(false)}
        title={form.title ?? ''}
        author={form.author ?? ''}
        photoUrl={displayedImageUrl}
        markdownText={form.markdownText ?? ''}
      />
    </PageShell>
  );
};

export default BlogPostEditPage;
