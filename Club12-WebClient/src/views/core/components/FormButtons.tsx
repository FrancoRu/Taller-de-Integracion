import { Button } from '@mui/material';

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
  <>
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
  </>
);

export default FormButtons;
