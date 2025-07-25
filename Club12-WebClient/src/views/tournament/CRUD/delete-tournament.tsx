import React, { useEffect } from 'react';
import Swal from 'sweetalert2';
import { GUID } from '@/modules/core/types/types';
import { useNavigate } from 'react-router-dom';

export const DeleteTournament: React.FC<{
  id: GUID;
  fn: (id: GUID) => Promise<void>;
  onClose: () => void;
}> = ({ id, fn, onClose }) => {
  const navigate = useNavigate();
  useEffect(() => {
    Swal.fire({
      title: '¿Está usted seguro de querer eliminar este torneo?',
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
            title: '¡Eliminado!',
            text: 'El torneo ha sido eliminado.',
            icon: 'success',
          }).then(() => {
            navigate('/');
          });
        } catch (error) {
          await Swal.fire({
            title: 'Error',
            text: 'Ocurrió un error al eliminar el torneo.',
            icon: 'error',
          });
        }
      }
      onClose();
    });
  }, [id, fn, onClose]);

  return null;
};
