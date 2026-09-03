import { useEffect, useMemo, useRef, useState } from 'react';
import { Box, Button, Stack, TextField, Typography, FormLabel } from '@mui/material';
import FieldInfoTooltip from '@/views/core/components/FieldInfoTooltip';
import JerseySvg from '@/views/core/components/JerseySvg';
import TeamLogo from '@/views/core/components/TeamLogo';
import { isHexColor } from '@/design/colorName';
import { JERSEY_STYLES, toJerseyStyle } from '@/design/jerseyStyles';
import type { TeamFormField, TeamFormState } from '@/views/team/teams.types';

/** Sensible defaults for the kit pickers when a team has no color yet. */
const DEFAULT_PRIMARY = '#1E5FCC';
const DEFAULT_SECONDARY = '#FFFFFF';
const DEFAULT_TERTIARY = '#0B0F17';

/** Debounce (ms) before a color-input drag actually updates form state. A
 * native color picker fires its change event continuously while the user
 * drags inside the color wheel — propagating every one of those straight to
 * form state re-renders the whole 20+ item style gallery (each its own SVG)
 * on every pixel of the drag, which is what made the picker feel laggy. */
const COLOR_INPUT_DEBOUNCE_MS = 120;

export interface TeamFormFieldsProps {
  withLogo: boolean;
  form: TeamFormState;
  onFieldChange: (field: TeamFormField, value: string) => void;
  onLogoChange: (file: File | null) => void;
}

/**
 * A native color input, styled to a reasonable, tappable size. Keeps its own
 * local value so the swatch itself always tracks the pointer instantly, but
 * debounces the actual `onChange` callback — see {@link COLOR_INPUT_DEBOUNCE_MS}.
 */
const ColorSwatchInput: React.FC<{
  value: string;
  onChange: (value: string) => void;
  ariaLabel: string;
}> = ({ value, onChange, ariaLabel }) => {
  const [localValue, setLocalValue] = useState(value);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Stay in sync when the value changes for a reason other than dragging
  // this same input (switching teams, the "Quitar" button, an external reset).
  useEffect(() => {
    setLocalValue(value);
  }, [value]);

  useEffect(
    () => () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    },
    []
  );

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const next = event.target.value;
    setLocalValue(next);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => onChange(next), COLOR_INPUT_DEBOUNCE_MS);
  };

  return (
    <Box
      component="input"
      type="color"
      aria-label={ariaLabel}
      value={localValue}
      onChange={handleChange}
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
};

/**
 * The escudo + name + code + kit-designer fields shared by every place a
 * team gets created or edited (the standalone Equipos admin form and the
 * "nuevo equipo" path of the tournament enrollment dialog) — a single
 * source of truth for team identity validation so a team created from
 * either entry point always ends up with the same required fields.
 */
export default function TeamFormFields({
  withLogo,
  form,
  onFieldChange,
  onLogoChange,
}: TeamFormFieldsProps) {
  const primaryValue = isHexColor(form.shirtColor)
    ? form.shirtColor
    : DEFAULT_PRIMARY;
  const hasSecondary = isHexColor(form.shirtSecondaryColor);
  const secondaryValue = hasSecondary
    ? form.shirtSecondaryColor
    : DEFAULT_SECONDARY;
  const hasTertiary = isHexColor(form.shirtTertiaryColor);
  const tertiaryValue = hasTertiary
    ? form.shirtTertiaryColor
    : DEFAULT_TERTIARY;
  const selectedStyle = toJerseyStyle(form.jerseyStyle);
  const previewSecondary = hasSecondary ? secondaryValue : undefined;
  const previewTertiary = hasTertiary ? tertiaryValue : undefined;
  const selectedUsesTertiary = Boolean(
    JERSEY_STYLES.find(option => option.value === selectedStyle)?.usesTertiary
  );

  // Preview a freshly picked escudo file immediately (object URL), falling back
  // to the team's stored logo. The URL is revoked when the file changes or the
  // component unmounts so it doesn't leak.
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

  // Memoized separately from the rest of the form: this grid renders one
  // JerseySvg per template (25+), so it should only re-render when a color
  // or the selected style actually changes — not on every keystroke in the
  // name/code fields above it.
  const styleGallery = useMemo(
    () =>
      JERSEY_STYLES.map(option => {
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
              tertiaryColor={previewTertiary}
              style={option.value}
              size={44}
              title={option.label}
            />
            <Typography variant="caption" sx={{ color: 'text.primary' }}>
              {option.label}
            </Typography>
          </Box>
        );
      }),
    [primaryValue, previewSecondary, previewTertiary, selectedStyle, onFieldChange]
  );

  return (
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
          <FormLabel required>Escudo</FormLabel>
          <TeamLogo teamName={form.name || '—'} logoUrl={displayedLogoUrl} size={88} />
          <Button
            variant="outlined"
            component="label"
            size="small"
            color={displayedLogoUrl ? 'primary' : 'error'}
          >
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
          onFieldChange('threeLetterCode', e.target.value.toUpperCase().slice(0, 3))
        }
        required
        fullWidth
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
                onChange={value => onFieldChange('shirtSecondaryColor', value)}
              />
              <Box sx={{ flex: 1 }}>
                <Stack direction="row" spacing={0} sx={{ alignItems: 'center' }}>
                  <Typography variant="body2">Color secundario</Typography>
                  {!hasSecondary && <FieldInfoTooltip title="Se genera automáticamente si lo dejás vacío." />}
                </Stack>
                {hasSecondary && (
                  <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                    {secondaryValue.toUpperCase()}
                  </Typography>
                )}
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

            <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <ColorSwatchInput
                ariaLabel="Color terciario"
                value={tertiaryValue}
                onChange={value => onFieldChange('shirtTertiaryColor', value)}
              />
              <Box sx={{ flex: 1 }}>
                <Stack direction="row" spacing={0} sx={{ alignItems: 'center' }}>
                  <Typography variant="body2">Color terciario</Typography>
                  {!hasTertiary && <FieldInfoTooltip title="Solo lo usan algunos modelos." />}
                </Stack>
                {hasTertiary && (
                  <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                    {tertiaryValue.toUpperCase()}
                  </Typography>
                )}
              </Box>
              {hasTertiary && (
                <Button
                  size="small"
                  onClick={() => onFieldChange('shirtTertiaryColor', '')}
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
              tertiaryColor={previewTertiary}
              style={selectedStyle}
              number={10}
              size={96}
              title="Vista previa de la camiseta"
            />
            {selectedUsesTertiary && !hasTertiary && (
              <Typography variant="caption" sx={{ color: 'text.secondary', textAlign: 'center', mt: 0.5 }}>
                Este modelo usa un color terciario
              </Typography>
            )}
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
          {styleGallery}
        </Box>
      </Box>
    </Stack>
  );
}
