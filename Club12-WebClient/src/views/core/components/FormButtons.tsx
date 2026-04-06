import { Button, Stack } from '@mui/material';

interface FormButtonsProps {
  onCancel: () => void;
  onConfirm: () => void;
  confirmLabel: string;
  disabled?: boolean;
}

const FormButtons: React.FC<FormButtonsProps> = ({
  onCancel,
  onConfirm,
  confirmLabel,
  disabled = false,
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
      disabled={disabled}
    >
      {confirmLabel}
    </Button>
  </Stack>
);

export default FormButtons;
