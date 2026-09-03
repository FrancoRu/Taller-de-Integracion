import React from 'react';
import { Typography, Box, Paper, Divider } from '@mui/material';
import {
  regulationBlocks,
  regulationIntro,
} from '@/views/home/information/regulationContent';
import PageShell from '@/views/core/components/PageShell';
import {
  DEFAULT_PAGE_METADATA,
  usePageMetadata,
} from '@/modules/core/utils/pageMetadata';

const Regulation: React.FC = () => {
  usePageMetadata({
    ...DEFAULT_PAGE_METADATA,
    title: 'Reglamento',
    description:
      'Reglamento oficial de la Liga de Básquet Libre Club 12 "La Vuelta": ' +
      'normas, sanciones y funcionamiento.',
  });

  return (
    <PageShell maxWidth="md">
      <Paper component="section" elevation={2} sx={{ p: { xs: 3, md: 6 } }}>
        <Typography
          variant="h3"
          component="h1"
          align="center"
          sx={{ fontWeight: 700 }}
        >
          Reglamento
        </Typography>
        <Typography
          variant="subtitle1"
          component="p"
          align="center"
          color="text.secondary"
          sx={{ mb: 4 }}
        >
          Liga de Básquet Libre Club 12 &quot;La Vuelta&quot;
        </Typography>

        <Typography sx={{ mb: 4, lineHeight: 1.8 }}>
          {regulationIntro}
        </Typography>

        {regulationBlocks.map((block, index) => {
          switch (block.kind) {
            case 'section':
              return (
                <Box key={index} sx={{ mt: 5, mb: 2 }}>
                  <Divider sx={{ mb: 2 }} />
                  <Typography
                    variant="h5"
                    component="h2"
                    sx={{ fontWeight: 700, letterSpacing: 0.5 }}
                  >
                    {block.text}
                  </Typography>
                </Box>
              );
            case 'article':
              return (
                <Box key={index} sx={{ mb: 2.5 }}>
                  <Typography
                    component="span"
                    sx={{ fontWeight: 700, mr: 1 }}
                  >
                    {block.label}.
                  </Typography>
                  <Typography component="span" sx={{ lineHeight: 1.8 }}>
                    {block.text}
                  </Typography>
                </Box>
              );
            case 'list':
              return (
                <Box
                  key={index}
                  component="ul"
                  sx={{ mb: 2.5, pl: 4, lineHeight: 1.8 }}
                >
                  {block.items.map((item, itemIndex) => (
                    <li key={itemIndex}>
                      <Typography component="span">{item}</Typography>
                    </li>
                  ))}
                </Box>
              );
            default:
              return null;
          }
        })}
      </Paper>
    </PageShell>
  );
};

export default Regulation;
