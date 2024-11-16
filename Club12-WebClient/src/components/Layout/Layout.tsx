import { AppBar, Toolbar, Typography, Container, Box } from '@mui/material'
import { Link } from 'react-router-dom'

export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" component={Link} to="/" sx={{ textDecoration: 'none', color: 'inherit' }}>
            Dashboard
          </Typography>
          <Box sx={{ flexGrow: 1 }} />
          <Box>
            <Typography component={Link} to="/login" sx={{ textDecoration: 'none', color: 'inherit', marginLeft: 2 }}>
              Login
            </Typography>
          </Box>
        </Toolbar>
      </AppBar>
      <Container component="main" sx={{ mt: 4, mb: 4, flexGrow: 1 }}>
        {children}
      </Container>
      <Box component="footer" sx={{ py: 3, px: 2, mt: 'auto', backgroundColor: (theme) => theme.palette.grey[200] }}>
        <Typography variant="body2" color="text.secondary" align="center">
          © {new Date().getFullYear()} Your Company Name
        </Typography>
      </Box>
    </Box>
  )
}