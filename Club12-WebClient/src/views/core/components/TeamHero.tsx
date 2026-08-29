import type { ReactNode } from 'react';
import { Box, Typography } from '@mui/material';
import { resolveShirtColor } from '@/design/colorName';
import { toJerseyStyle } from '@/design/jerseyStyles';
import JerseySvg from '@/views/core/components/JerseySvg';
import TeamLogo from '@/views/core/components/TeamLogo';

export interface TeamHeroProps {
  name: string;
  code?: string;
  logoUrl?: string | null;
  /** Primary shirt color as a `#rrggbb` hex; tints the band. */
  shirtColor?: string | null;
  /** Secondary shirt color for the jersey pattern/trim. */
  secondaryColor?: string | null;
  /** Kit template; unknown values fall back to `solid`. */
  jerseyStyle?: string | null;
  /** Optional content rendered below the identity row (tabs, meta, etc.). */
  children?: ReactNode;
}

/** Turns a `#rrggbb` hex into an `rgba()` string at the given alpha. */
const hexToRgba = (hex: string, alpha: number): string => {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
};

/**
 * The public header band for a team page. The background layers the club's
 * escudo (large, blurred, dimmed) beneath a linear-gradient tint derived from
 * the team's shirt color, so the band takes on the team identity. A dark scrim
 * keeps the foreground — logo, name, code and jersey kit — AA-legible over any
 * background, and it degrades gracefully to a flat tint when no logo is set.
 */
export default function TeamHero({
  name,
  code,
  logoUrl,
  shirtColor,
  secondaryColor,
  jerseyStyle,
  children,
}: TeamHeroProps) {
  const { fill } = resolveShirtColor(shirtColor);
  const tint = `linear-gradient(135deg, ${hexToRgba(fill, 0.75)} 0%, ${hexToRgba(
    fill,
    0.35
  )} 55%, ${hexToRgba(fill, 0.15)} 100%)`;

  return (
    <Box
      component="header"
      sx={{
        position: 'relative',
        overflow: 'hidden',
        borderRadius: 2,
        px: { xs: 2, sm: 4 },
        py: { xs: 3, sm: 4 },
        color: '#fff',
      }}
    >
      {/* Blurred escudo backdrop (only when a logo exists). */}
      {logoUrl && (
        <Box
          aria-hidden="true"
          sx={{
            position: 'absolute',
            inset: 0,
            backgroundImage: `url(${logoUrl})`,
            backgroundSize: 'cover',
            backgroundPosition: 'center',
            filter: 'blur(28px)',
            opacity: 0.35,
            transform: 'scale(1.2)',
          }}
        />
      )}
      {/* Team-color tint overlay. */}
      <Box
        aria-hidden="true"
        sx={{ position: 'absolute', inset: 0, backgroundImage: tint }}
      />
      {/* Dark scrim so foreground text stays legible over any backdrop. */}
      <Box
        aria-hidden="true"
        sx={{
          position: 'absolute',
          inset: 0,
          backgroundColor: 'rgba(11, 15, 23, 0.45)',
        }}
      />

      {/* Foreground identity row. */}
      <Box
        sx={{
          position: 'relative',
          display: 'flex',
          flexDirection: { xs: 'column', sm: 'row' },
          alignItems: 'center',
          gap: { xs: 2, sm: 3 },
          textAlign: { xs: 'center', sm: 'left' },
        }}
      >
        <TeamLogo teamName={name} logoUrl={logoUrl} size={80} />
        <Box sx={{ flex: 1 }}>
          <Typography
            variant="h3"
            component="h1"
            sx={{ color: '#fff', lineHeight: 1.1 }}
          >
            {name}
          </Typography>
          {code && (
            <Typography
              variant="subtitle1"
              sx={{ color: 'rgba(255, 255, 255, 0.85)', letterSpacing: '0.08em' }}
            >
              {code}
            </Typography>
          )}
        </Box>
        <JerseySvg
          color={shirtColor}
          secondaryColor={secondaryColor}
          style={toJerseyStyle(jerseyStyle)}
          size={72}
          title={`Camiseta de ${name}`}
        />
      </Box>

      {children && (
        <Box sx={{ position: 'relative', mt: { xs: 2, sm: 3 } }}>{children}</Box>
      )}
    </Box>
  );
}
