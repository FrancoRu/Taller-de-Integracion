import { Typography } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import ShowPosts from '@/views/blogPost/showPosts';
import {
  DEFAULT_PAGE_METADATA,
  usePageMetadata,
} from '@/modules/core/utils/pageMetadata';

export default function BlogListPage() {
  usePageMetadata({
    ...DEFAULT_PAGE_METADATA,
    title: 'Novedades',
    description:
      'Últimas noticias, resultados y novedades de la liga de básquet ' +
      'Club 12.',
  });

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
