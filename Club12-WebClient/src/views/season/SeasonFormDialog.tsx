import { Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField } from '@mui/material';
import FormButtons from '@/views/core/components/FormButtons';

export type SeasonFormState = {
  name: string;
  year: string;
};

interface SeasonFormDialogProps {
  open: boolean;
  title: string;
  confirmLabel: string;
  form: SeasonFormState;
  submitting: boolean;
  onFieldChange: (field: keyof SeasonFormState, value: string) => void;
  onClose: () => void;
  onConfirm: () => void;
}

/**
 * The name/year fields shared by every place a season gets created or
 * edited — the standalone Temporadas admin form and the season's own
 * detail page.
 */
const SeasonFormDialog: React.FC<SeasonFormDialogProps> = ({
  open,
  title,
  confirmLabel,
  form,
  submitting,
  onFieldChange,
  onClose,
  onConfirm,
}) => {
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
            label="Año"
            type="number"
            value={form.year}
            onChange={e => onFieldChange('year', e.target.value)}
            fullWidth
          />
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

export default SeasonFormDialog;
