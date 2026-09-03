import { Typography, Paper, Box, Chip } from '@mui/material';
import DownloadIcon from '@mui/icons-material/Download';
import AssignmentTurnedInIcon from '@mui/icons-material/AssignmentTurnedIn';
import PageShell from '@/views/core/components/PageShell';
import {
  DEFAULT_PAGE_METADATA,
  usePageMetadata,
} from '@/modules/core/utils/pageMetadata';

const steps = [
  {
    label: 'Descargar',
    description: 'Descargá la ficha médica en formato PDF.',
  },
  {
    label: 'Completar',
    description:
      'Llevala a tu médico para que la complete y la firme.',
  },
  {
    label: 'Entregar',
    description:
      'Entregá la ficha completa a la organización de Club 12 antes de tu primer partido.',
  },
];

export default function MedicalRecord() {
  usePageMetadata({
    ...DEFAULT_PAGE_METADATA,
    title: 'Ficha médica',
    description:
      'Descargá la ficha médica obligatoria para jugar en la liga Club 12 ' +
      'y conocé cómo presentarla.',
  });

  return (
    <PageShell maxWidth="md">
      <Paper component="section" elevation={2} sx={{ p: { xs: 3, md: 5 } }}>
        <Chip
          icon={<AssignmentTurnedInIcon />}
          label="Requisito obligatorio para jugar"
          color="primary"
          variant="outlined"
          sx={{ mb: 2 }}
        />
        <Typography variant="h3" component="h1" sx={{ fontWeight: 700, mb: 1 }}>
          Ficha médica
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 4 }}>
          Todo jugador debe presentar su ficha médica completa antes de
          disputar partidos oficiales de la liga.
        </Typography>

        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' },
            gap: 3,
            mb: 4,
          }}
        >
          {steps.map((step, index) => (
            <Box key={step.label}>
              <Typography
                variant="h4"
                component="span"
                sx={{ display: 'block', fontWeight: 700, color: 'primary.main', mb: 0.5 }}
              >
                {index + 1}
              </Typography>
              <Typography sx={{ fontWeight: 600, mb: 0.5 }}>
                {step.label}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {step.description}
              </Typography>
            </Box>
          ))}
        </Box>

        <Box
          component="a"
          href="/documents/ficha-medica-club12.pdf"
          download
          sx={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 1,
            px: 3,
            py: 1.5,
            borderRadius: 1,
            bgcolor: 'primary.main',
            color: 'primary.contrastText',
            fontWeight: 600,
            textDecoration: 'none',
          }}
        >
          <DownloadIcon fontSize="small" />
          Descargar ficha médica
        </Box>
      </Paper>
    </PageShell>
  );
}
