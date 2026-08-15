import { useCallback, useState } from 'react';
import {
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Typography,
} from '@mui/material';
import Swal from 'sweetalert2';
import theme from '@/theme';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { IPlayerSanctionDeletePageProps } from '@/modules/playerSanction/type/playerSanction.d';
import FormButtons from '@/views/core/components/FormButtons';

const PlayerSanctionDeletePage: React.FC<IPlayerSanctionDeletePageProps> = ({
  open,
  sanction,
  onClose,
  onDeleted,
}) => {
  const { deletePlayerSanction } = usePlayerSanction();
  const [submitting, setSubmitting] = useState(false);

  const handleClose = useCallback(() => {
    if (submitting) {
      return;
    }

    onClose();
  }, [onClose, submitting]);

  const handleDelete = useCallback(async () => {
    if (!sanction) {
      return;
    }

    setSubmitting(true);
    await deletePlayerSanction(sanction.id);
    setSubmitting(false);

    await Swal.fire({
      title: '¡Eliminada!',
      text: 'La sanción ha sido eliminada.',
      icon: 'success',
      confirmButtonColor: theme.palette.primary.main,
    });

    onClose();
    onDeleted?.();
  }, [deletePlayerSanction, onClose, onDeleted, sanction]);

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="xs" fullWidth>
      <DialogTitle>Eliminar sanción</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary">
          ¿Está usted seguro de querer eliminar esta sanción? Usted no podrá
          revertir este cambio.
        </Typography>
      </DialogContent>
      <DialogActions>
        <FormButtons
          onCancel={handleClose}
          onConfirm={() => void handleDelete()}
          confirmLabel="Eliminar"
          disabled={submitting}
        />
      </DialogActions>
    </Dialog>
  );
};

export default PlayerSanctionDeletePage;
