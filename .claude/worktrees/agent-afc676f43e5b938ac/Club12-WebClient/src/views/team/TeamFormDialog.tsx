import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField } from '@mui/material';
import FormButtons from '@/views/core/components/FormButtons';
import type { TeamFormState } from '@/views/team/teams.types';

interface TeamFormDialogProps {
  open: boolean;
  title: string;
  confirmLabel: string;
  withLogo: boolean;
  form: TeamFormState;
  submitting: boolean;
  onFieldChange: (
    field: 'name' | 'threeLetterCode' | 'shirtColor',
    value: string
  ) => void;
  onLogoChange: (file: File | null) => void;
  onClose: () => void;
  onConfirm: () => void;
}

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
}) => (
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
        <TextField
          label="Color camiseta"
          value={form.shirtColor}
          onChange={e => onFieldChange('shirtColor', e.target.value)}
          fullWidth
        />
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

export default TeamFormDialog;
