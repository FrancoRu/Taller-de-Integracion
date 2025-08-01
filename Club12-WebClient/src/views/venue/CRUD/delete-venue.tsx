import { GUID } from '@/modules/core/types/types';
import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Swal from 'sweetalert2';

export const DeleteVenue: React.FC<{
  id: GUID;
  route: string;
  fn: (id: GUID) => Promise<void>;
  onClose: () => void;
}> = ({ id, route, fn, onClose }) => {
  const navigate = useNavigate();
  useEffect(() => {
    Swal.fire({
      title: '¿Está usted seguro de querer eliminar esta cancha?',
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
        navigate(route);
      }
      onClose();
    });
  }, [id, fn, onClose]);

  return null;
};
