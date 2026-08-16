import { Container, Typography } from '@mui/material';
import ShowPosts from '@/views/blogPost/showPosts';

export default function BlogListPage() {
  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Typography variant="h4" component="h1" fontWeight="bold" mb={1}>
        Novedades
      </Typography>
      <Typography variant="body1" color="text.secondary" mb={4}>
        Últimas noticias de la liga Club 12.
      </Typography>
      <ShowPosts />
    </Container>
  );
}
