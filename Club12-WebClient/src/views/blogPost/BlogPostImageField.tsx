import { Box, Button, Typography } from '@mui/material';
import SportsBasketballIcon from '@mui/icons-material/SportsBasketball';

const IMAGE_PREVIEW_HEIGHT = 200;

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
          if (selectedFile) {
            onFileSelect(selectedFile);
          }
          event.target.value = '';
        }}
      />
    </Button>
  </Box>
);

export default BlogPostImageField;
