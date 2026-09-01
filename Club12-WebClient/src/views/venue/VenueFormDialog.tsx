import { Dialog, DialogActions, DialogContent, DialogTitle } from '@mui/material';
import FormButtons from '@/views/core/components/FormButtons';
import VenueFormFields from '@/views/venue/VenueFormFields';
import type { VenueFormField, VenueFormState } from '@/views/venue/venues.types';

interface VenueFormDialogProps {
  open: boolean;
  title: string;
  confirmLabel: string;
  withPhoto: boolean;
  form: VenueFormState;
  submitting: boolean;
  onFieldChange: (field: VenueFormField, value: string) => void;
  onPhotoChange: (file: File | null) => void;
  onClose: () => void;
  onConfirm: () => void;
}

const VenueFormDialog: React.FC<VenueFormDialogProps> = ({
  open,
  title,
  confirmLabel,
  withPhoto,
  form,
  submitting,
  onFieldChange,
  onPhotoChange,
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
        <VenueFormFields
          withPhoto={withPhoto}
          form={form}
          onFieldChange={onFieldChange}
          onPhotoChange={onPhotoChange}
        />
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

export default VenueFormDialog;
