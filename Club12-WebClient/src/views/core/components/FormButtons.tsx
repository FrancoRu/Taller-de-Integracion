import { Button, Stack } from '@mui/material';

interface FormButtonsProps {
  onCancel: () => void;
  onConfirm: () => void;
  confirmLabel: string;
  /** Disables BOTH buttons — for a submit actually in flight. */
  disabled?: boolean;
  /**
   * Disables ONLY the confirm button — for a validation or structural block
   * (e.g. required fields empty, the record is in a state that no longer
   * accepts this action). Cancel must always stay clickable, or a blocked
   * form becomes a dead end with no way to leave the page.
   */
  confirmDisabled?: boolean;
}

const FormButtons: React.FC<FormButtonsProps> = ({
  onCancel,
  onConfirm,
  confirmLabel,
  disabled = false,
  confirmDisabled = false,
}) => (
  <Stack direction="row" spacing={1.5}>
    <Button
      variant="contained"
      color="primary"
      onClick={onCancel}
      disabled={disabled}
    >
      Cancelar
    </Button>
    <Button
      variant="contained"
      color="primary"
      onClick={onConfirm}
      disabled={disabled || confirmDisabled}
    >
      {confirmLabel}
    </Button>
  </Stack>
);

export default FormButtons;
