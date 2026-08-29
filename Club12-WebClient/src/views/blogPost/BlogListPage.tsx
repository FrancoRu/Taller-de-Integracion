import { Typography } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import ShowPosts from '@/views/blogPost/showPosts';

export default function BlogListPage() {
  return (
    <PageShell title="Novedades">
      <Typography
        variant="body1"
        sx={{
          color: 'text.secondary',
          mb: 4,
        }}
      >
        Últimas noticias de la liga Club 12.
      </Typography>
      <ShowPosts />
    </PageShell>
  );
}
