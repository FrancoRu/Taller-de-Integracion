import { Box, Container, IconButton, Stack, Typography } from '@mui/material';
import { InstagramIcon, FacebookIcon } from '@/views/core/MUI/icons/icons';
import { LOGO_BACKGROUND_COLOR } from '@/theme';

const SOCIAL_LINKS = [
  {
    label: 'Instagram',
    href: 'https://www.instagram.com/club12basquet/?hl=es-la',
    icon: <InstagramIcon />,
  },
  {
    label: 'Facebook',
    href: 'https://www.facebook.com/Club12LaVuelta/?locale=es_LA',
    icon: <FacebookIcon />,
  },
];

const Footer = () => {
  const year = new Date().getFullYear();

  return (
    <Box
      component="footer"
      sx={{
        bgcolor: 'background.paper',
        borderTop: '1px solid',
        borderColor: 'divider',
        color: 'text.primary',
        mt: 8,
      }}
    >
      <Container maxWidth="lg" sx={{ py: 3 }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ alignItems: 'center', justifyContent: 'space-between' }}
        >
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              bgcolor: LOGO_BACKGROUND_COLOR,
              borderRadius: 1.5,
              p: 0.5,
            }}
          >
            <Box
              component="img"
              src="/assets/logo-club12.png"
              alt="Club 12"
              sx={{ height: 48, width: 'auto', display: 'block' }}
            />
          </Box>

          <Typography
            variant="body2"
            sx={{ color: 'rgba(255,255,255,0.7)', textAlign: 'center' }}
          >
            La liga de básquet amateur con más historia de la zona.
          </Typography>

          <Stack direction="row" spacing={1}>
            {SOCIAL_LINKS.map((social) => (
              <IconButton
                key={social.label}
                component="a"
                href={social.href}
                target="_blank"
                rel="noopener noreferrer"
                aria-label={social.label}
                sx={{
                  color: '#fff',
                  bgcolor: 'rgba(255,255,255,0.08)',
                  '&:hover': { bgcolor: 'primary.main' },
                }}
              >
                {social.icon}
              </IconButton>
            ))}
          </Stack>
        </Stack>

        <Typography
          variant="caption"
          sx={{
            display: 'block',
            textAlign: 'center',
            color: 'rgba(255,255,255,0.5)',
            mt: 4,
          }}
        >
          © {year} Club 12 &quot;La Vuelta&quot;
        </Typography>
      </Container>
    </Box>
  );
};

export default Footer;
