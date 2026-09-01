import { Dialog, DialogActions, DialogContent, DialogTitle } from '@mui/material';
import FormButtons from '@/views/core/components/FormButtons';
import TeamFormFields from '@/views/team/TeamFormFields';
import type { TeamFormField, TeamFormState } from '@/views/team/teams.types';

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
  return (
    <Dialog
      open={open}
      onClose={() => !submitting && onClose()}
      fullWidth
      maxWidth="sm"
    >
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <TeamFormFields
          withLogo={withLogo}
          form={form}
          onFieldChange={onFieldChange}
          onLogoChange={onLogoChange}
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

export default TeamFormDialog;
