import { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import FormButtons from '@/views/core/components/FormButtons';
import JerseySvg from '@/views/core/components/JerseySvg';
import TeamLogo from '@/views/core/components/TeamLogo';
import { isHexColor } from '@/design/colorName';
import { JERSEY_STYLES, toJerseyStyle } from '@/design/jerseyStyles';
import type { TeamFormField, TeamFormState } from '@/views/team/teams.types';

/** Sensible defaults for the kit pickers when a team has no color yet. */
const DEFAULT_PRIMARY = '#1E5FCC';
const DEFAULT_SECONDARY = '#FFFFFF';

interface TeamFormDialogProps {
  open: boolean;
  title: string;
  confirmLabel: string;
  withLogo: boolean;
  form: TeamFormState;
  submitting: boolean;
  onFieldChange: (field: TeamFormField, value: string) => void;
  onLogoChange: (file: File | null) => void;
  onClose: () => void;
  onConfirm: () => void;
}

/** A native color input, styled to a reasonable, tappable size. */
const ColorSwatchInput: React.FC<{
  value: string;
  onChange: (value: string) => void;
  ariaLabel: string;
}> = ({ value, onChange, ariaLabel }) => (
  <Box
    component="input"
    type="color"
    aria-label={ariaLabel}
    value={value}
    onChange={(event: React.ChangeEvent<HTMLInputElement>) =>
      onChange(event.target.value)
    }
    sx={{
      width: 56,
      height: 40,
      p: 0,
      border: '1px solid',
      borderColor: 'divider',
      borderRadius: 1,
      background: 'none',
      cursor: 'pointer',
    }}
  />
);

const TeamFormDialog: React.FC<TeamFormDialogProps> = ({
  open,
  title,
  confirmLabel,
  withLogo,
  form,
  submitting,
  onFieldChange,
  onLogoChange,
  onClose,
  onConfirm,
}) => {
  const primaryValue = isHexColor(form.shirtColor)
    ? form.shirtColor
    : DEFAULT_PRIMARY;
  const hasSecondary = isHexColor(form.shirtSecondaryColor);
  const secondaryValue = hasSecondary
    ? form.shirtSecondaryColor
    : DEFAULT_SECONDARY;
  const selectedStyle = toJerseyStyle(form.jerseyStyle);
  const previewSecondary = hasSecondary ? secondaryValue : undefined;

  // Preview a freshly picked escudo file immediately (object URL), falling back
  // to the team's stored logo. The URL is revoked when the file changes or the
  // dialog unmounts so it doesn't leak.
  const [logoPreview, setLogoPreview] = useState('');
  useEffect(() => {
    if (!form.logo) {
      setLogoPreview('');
      return;
    }
    const url = URL.createObjectURL(form.logo);
    setLogoPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [form.logo]);
  const displayedLogoUrl = logoPreview || form.logoUrl;

  return (
    <Dialog
      open={open}
      onClose={() => !submitting && onClose()}
      fullWidth
      maxWidth="sm"
    >
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {/* The escudo is the team's primary identity, so it leads the form. */}
          {withLogo && (
            <Box
              sx={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: 1,
              }}
            >
              <TeamLogo teamName={form.name || '—'} logoUrl={displayedLogoUrl} size={88} />
              <Button variant="outlined" component="label" size="small">
                {form.logoUrl || form.logo ? 'Cambiar escudo' : 'Seleccionar escudo'}
                <input
                  hidden
                  type="file"
                  accept="image/*"
                  onChange={event => {
                    const selectedFile = event.target.files?.[0] ?? null;
                    onLogoChange(selectedFile);
                  }}
                />
              </Button>
              {form.logo && (
                <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                  {form.logo.name}
                </Typography>
              )}
            </Box>
          )}

          <TextField
            label="Nombre"
            value={form.name}
            onChange={e => onFieldChange('name', e.target.value)}
            required
            fullWidth
          />
          <TextField
            label="Código (3 letras)"
            value={form.threeLetterCode}
            onChange={e =>
              onFieldChange(
                'threeLetterCode',
                e.target.value.toUpperCase().slice(0, 3)
              )
            }
            required
            fullWidth
            helperText="Sigla de 3 letras del equipo (ej. CAC)."
            slotProps={{ htmlInput: { maxLength: 3 } }}
          />

          {/* Kit designer: colors, template gallery and a live preview. */}
          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Camiseta
            </Typography>

            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={2}
              sx={{ alignItems: { xs: 'stretch', sm: 'flex-start' } }}
            >
              <Stack spacing={2} sx={{ flex: 1 }}>
                <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
                  <ColorSwatchInput
                    ariaLabel="Color primario"
                    value={primaryValue}
                    onChange={value => onFieldChange('shirtColor', value)}
                  />
                  <Box>
                    <Typography variant="body2">Color primario</Typography>
                    <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                      {primaryValue.toUpperCase()}
                    </Typography>
                  </Box>
                </Stack>

                <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
                  <ColorSwatchInput
                    ariaLabel="Color secundario"
                    value={secondaryValue}
                    onChange={value =>
                      onFieldChange('shirtSecondaryColor', value)
                    }
                  />
                  <Box sx={{ flex: 1 }}>
                    <Typography variant="body2">Color secundario</Typography>
                    <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                      {hasSecondary ? secondaryValue.toUpperCase() : 'Automático'}
                    </Typography>
                  </Box>
                  {hasSecondary && (
                    <Button
                      size="small"
                      onClick={() => onFieldChange('shirtSecondaryColor', '')}
                    >
                      Quitar
                    </Button>
                  )}
                </Stack>
              </Stack>

              {/* Live preview of the current kit with an example dorsal. */}
              <Box
                sx={{
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  minWidth: 120,
                }}
              >
                <JerseySvg
                  color={primaryValue}
                  secondaryColor={previewSecondary}
                  style={selectedStyle}
                  number={10}
                  size={96}
                  title="Vista previa de la camiseta"
                />
                <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                  Vista previa
                </Typography>
              </Box>
            </Stack>

            <Typography variant="subtitle2" sx={{ mt: 2, mb: 1 }}>
              Modelo de camiseta
            </Typography>
            <Box
              role="radiogroup"
              aria-label="Modelo de camiseta"
              sx={{
                display: 'grid',
                gridTemplateColumns: {
                  xs: 'repeat(3, 1fr)',
                  sm: 'repeat(4, 1fr)',
                },
                gap: 1,
              }}
            >
              {JERSEY_STYLES.map(option => {
                const selected = option.value === selectedStyle;
                return (
                  <Box
                    key={option.value}
                    component="button"
                    type="button"
                    role="radio"
                    aria-checked={selected}
                    aria-label={option.label}
                    onClick={() => onFieldChange('jerseyStyle', option.value)}
                    sx={{
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'center',
                      gap: 0.5,
                      p: 1,
                      cursor: 'pointer',
                      background: 'none',
                      // A native <button> defaults to the browser's black text
                      // color, which is invisible on the dark dialog — force the
                      // theme ink so the model labels stay legible.
                      color: 'text.primary',
                      borderRadius: 1,
                      border: '2px solid',
                      borderColor: selected ? 'primary.main' : 'divider',
                      boxShadow: selected ? 2 : 0,
                      '&:hover': { borderColor: 'primary.main' },
                    }}
                  >
                    <JerseySvg
                      color={primaryValue}
                      secondaryColor={previewSecondary}
                      style={option.value}
                      size={44}
                      title={option.label}
                    />
                    <Typography variant="caption" sx={{ color: 'text.primary' }}>
                      {option.label}
                    </Typography>
                  </Box>
                );
              })}
            </Box>
          </Box>

        </Stack>
      </DialogContent>
      <DialogActions>
        <FormButtons
          onCancel={onClose}
          onConfirm={onConfirm}
          confirmLabel={confirmLabel}
          disabled={submitting}
        />
      </DialogActions>
    </Dialog>
  );
};

export default TeamFormDialog;
