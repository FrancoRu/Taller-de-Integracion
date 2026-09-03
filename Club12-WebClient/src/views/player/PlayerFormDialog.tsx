import { Dialog, DialogActions, DialogContent, DialogTitle } from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import FormButtons from '@/views/core/components/FormButtons';
import PlayerFormFields, {
  PlayerDorsalFieldConfig,
} from '@/views/player/PlayerFormFields';
import type { ITeamResponse } from '@/modules/team/type/team.d';
import type { PlayerFormField, PlayerFormState } from '@/views/player/players.types';

interface PlayerFormDialogProps {
  open: boolean;
  title: string;
  confirmLabel: string;
  form: PlayerFormState;
  submitting: boolean;
  /** Disables the confirm button beyond `submitting` (e.g. inline validation
   * errors) without also blocking the dialog's close affordance. */
  confirmDisabled?: boolean;
  showTeamSelect: boolean;
  teamOptions: ITeamResponse[];
  onTeamChange: (teamId: GUID) => void;
  dorsalField?: PlayerDorsalFieldConfig;
  onFieldChange: (field: PlayerFormField, value: string) => void;
  onClose: () => void;
  onConfirm: () => void;
}

const PlayerFormDialog: React.FC<PlayerFormDialogProps> = ({
  open,
  title,
  confirmLabel,
  form,
  submitting,
  confirmDisabled,
  showTeamSelect,
  teamOptions,
  onTeamChange,
  dorsalField,
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
        <PlayerFormFields
          form={form}
          onFieldChange={onFieldChange}
          showTeamSelect={showTeamSelect}
          teamOptions={teamOptions}
          onTeamChange={onTeamChange}
          dorsalField={dorsalField}
        />
      </DialogContent>
      <DialogActions>
        <FormButtons
          onCancel={onClose}
          onConfirm={onConfirm}
          confirmLabel={confirmLabel}
          disabled={submitting}
          confirmDisabled={Boolean(confirmDisabled)}
        />
      </DialogActions>
    </Dialog>
  );
};

export default PlayerFormDialog;
