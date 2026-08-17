import { Box, Dialog, DialogContent, DialogTitle, IconButton, Typography } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';

interface BlogPostPreviewDialogProps {
  open: boolean;
  onClose: () => void;
  title: string;
  author: string;
  photoUrl?: string;
  markdownText: string;
}

/**
 * Renders the post the same way the public page (BlogPostDetailPage) would,
 * using whatever is currently in the form — including unsaved edits and a
 * freshly picked (not-yet-uploaded) image. A modal is used instead of
 * opening the real public URL in a new tab because the create form has no
 * post id to link to yet, and this way both forms share one "Vista previa"
 * behavior that always reflects the current draft.
 */
const BlogPostPreviewDialog: React.FC<BlogPostPreviewDialogProps> = ({
  open,
  onClose,
  title,
  author,
  photoUrl,
  markdownText,
}) => (
  <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
    <DialogTitle
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
      }}
    >
      Vista previa
      <IconButton onClick={onClose} aria-label="Cerrar vista previa">
        <CloseIcon />
      </IconButton>
    </DialogTitle>
    <DialogContent dividers>
      <Typography variant="h4" component="h1" sx={{ fontWeight: 700, mb: 1 }}>
        {title || 'Sin título'}
      </Typography>
      <Typography
        variant="subtitle1"
        component="p"
        sx={{ color: 'text.secondary', mb: 3 }}
      >
        {author || 'Autor sin definir'}
      </Typography>
      {photoUrl && (
        <Box
          component="img"
          src={photoUrl}
          alt={title}
          sx={{ width: '100%', borderRadius: 2, mb: 3 }}
        />
      )}
      <div dangerouslySetInnerHTML={{ __html: markdownText }} />
    </DialogContent>
  </Dialog>
);

export default BlogPostPreviewDialog;
