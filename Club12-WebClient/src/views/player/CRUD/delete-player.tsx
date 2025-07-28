import { GUID } from '@/modules/core/types/types';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Swal from 'sweetalert2';

export const DeletePlayer: React.FC<{
  id: GUID;
  fn: (id: GUID) => Promise<void>;
  onClose: () => void;
}> = ({ id, fn, onClose }) => {
  const navigate = useNavigate();
  useEffect(() => {
    Swal.fire({
      title: '¿Está usted seguro de querer eliminar este jugador?',
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
        navigate(`/${RoutesNavigationViews.Player}`);
      }
      onClose();
    });
  }, [id, fn, onClose]);

  return null;
};
