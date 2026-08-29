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
          <TextField
            label="Nombre"
            value={form.name}
            onChange={e => onFieldChange('name', e.target.value)}
            required
            fullWidth
          />
          <TextField
            label="Código"
            value={form.threeLetterCode}
            onChange={e => onFieldChange('threeLetterCode', e.target.value)}
            required
            fullWidth
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
                      borderRadius: 1,
                      border: '2px solid',
                      borderColor: selected ? 'primary.main' : 'divider',
                      boxShadow: selected ? 2 : 0,
                    }}
                  >
                    <JerseySvg
                      color={primaryValue}
                      secondaryColor={previewSecondary}
                      style={option.value}
                      size={44}
                      title={option.label}
                    />
                    <Typography variant="caption">{option.label}</Typography>
                  </Box>
                );
              })}
            </Box>
          </Box>

          {withLogo && (
            <Button variant="outlined" component="label">
              {form.logo ? `Logo: ${form.logo.name}` : 'Seleccionar logo'}
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
          )}
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
