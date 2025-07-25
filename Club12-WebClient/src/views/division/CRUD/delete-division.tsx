import React, { useEffect } from 'react';
import Swal from 'sweetalert2';
import { GUID } from '@/modules/core/types/types';

export const DeleteDivision: React.FC<{
  id: GUID;
  fn: (id: GUID) => Promise<void>;
  onClose: () => void;
}> = ({ id, fn, onClose }) => {
  useEffect(() => {
    Swal.fire({
      title: '¿Está usted seguro de querer eliminar esta división?',
      text: '¡Usted no podrá revertir este cambio!',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Sí, eliminar!',
      cancelButtonText: 'Cancelar',
    }).then(async result => {
      if (result.isConfirmed) {
        try {
          await fn(id);
          await Swal.fire({
            title: '¡Eliminada!',
            text: 'La división ha sido eliminada.',
            icon: 'success',
          });
        } catch (error) {
          await Swal.fire({
            title: 'Error',
            text: 'Ocurrió un error al eliminar la división.',
            icon: 'error',
          });
        }
      }
      onClose();
    });
  }, [id, fn, onClose]);

  return null;
};
