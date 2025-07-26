import { GUID } from '@/modules/core/types/types';
import { useEffect } from 'react';
import Swal from 'sweetalert2';

export const DeleteMatch: React.FC<{
  id: GUID;
  fn: (id: GUID) => Promise<void>;
  onClose: () => void;
}> = ({ id, fn, onClose }) => {
  useEffect(() => {
    Swal.fire({
      title: '¿Está usted seguro de querer eliminar este partido?',
      text: '¡Usted no podrá revertir este cambio!',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Sí, eliminar!',
      cancelButtonText: 'Cancelar',
    }).then(async result => {
      if (result.isConfirmed) {
        await fn(id);
      }
      onClose();
    });
  }, [id, fn, onClose]);

  return null;
};
