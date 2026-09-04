import { Box, Button, Typography } from '@mui/material';
import SportsBasketballIcon from '@mui/icons-material/SportsBasketball';
import { notifyWarning } from '@/modules/core/utils/confirmDialog';

const IMAGE_PREVIEW_HEIGHT = 200;

// nginx on the server rejects oversized request bodies (413), so we reject
// the file client-side before it ever reaches that limit.
const MAX_IMAGE_SIZE_BYTES = 1024 * 1024;

interface BlogPostImageFieldProps {
  /** Resolved image to display: an object URL for a newly picked file, or the post's saved photoUrl. */
  previewUrl?: string;
  /** Whether the post already has (or will have) an image, to switch the button's wording. */
  hasImage: boolean;
  onFileSelect: (file: File) => void;
}

/**
 * Featured-image preview + upload/change control for the blog post create and
 * edit forms. Mirrors the placeholder pattern used on the public post cards
 * (showPosts.tsx / home.tsx): a SportsBasketballIcon block when there is no
 * image yet, otherwise the image itself.
 */
const BlogPostImageField: React.FC<BlogPostImageFieldProps> = ({
  previewUrl,
  hasImage,
  onFileSelect,
}) => (
  <Box>
    <Typography variant="subtitle1" sx={{ mb: 1 }}>
      Imagen destacada
    </Typography>
    {previewUrl ? (
      <Box
        component="img"
        src={previewUrl}
        alt="Imagen destacada de la publicación"
        sx={{
          width: '100%',
          maxWidth: 400,
          height: IMAGE_PREVIEW_HEIGHT,
          objectFit: 'cover',
          borderRadius: 1,
          mb: 1,
          bgcolor: 'action.hover',
        }}
      />
    ) : (
      <Box
        sx={{
          width: '100%',
          maxWidth: 400,
          height: IMAGE_PREVIEW_HEIGHT,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          bgcolor: 'action.hover',
          color: 'text.disabled',
          borderRadius: 1,
          mb: 1,
        }}
      >
        <SportsBasketballIcon sx={{ fontSize: 48 }} />
      </Box>
    )}
    <Button variant="outlined" component="label">
      {hasImage ? 'Cambiar imagen' : 'Seleccionar imagen'}
      <input
        hidden
        type="file"
        accept="image/*"
        onChange={event => {
          const selectedFile = event.target.files?.[0];
          event.target.value = '';
          if (!selectedFile) {
            return;
          }
          if (selectedFile.size > MAX_IMAGE_SIZE_BYTES) {
            void notifyWarning({
              title: 'Imagen demasiado pesada',
              text: 'La imagen no puede superar 1 MB. Elegí una imagen más liviana.',
            });
            return;
          }
          onFileSelect(selectedFile);
        }}
      />
    </Button>
  </Box>
);

export default BlogPostImageField;
