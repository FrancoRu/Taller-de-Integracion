import { GUID } from '@/modules/core/types/types';
import { useStage } from '@/modules/stage/hook/stage.hook';
import React, { useEffect } from 'react';
import Swal from 'sweetalert2';

export const GenerateStage: React.FC<{
  id: GUID;
  onClose: () => void;
}> = ({ id, onClose }) => {
  const { generateStagesAutomatically, getStagesByFilters } = useStage();

  useEffect(() => {
    Swal.fire({
      title: '¿Estás seguro?',
      text: 'Se generarán las fases automáticamente. Una vez realizada esta acción, no podrás volver a utilizar esta función para esta división.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Sí, generar fáses',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      showLoaderOnConfirm: true,
      preConfirm: async () => {
        try {
          const result = await generateStagesAutomatically(id);

          if (!result) {
            Swal.showValidationMessage(
              'No se pudieron generar las fases. Verifica si ya han sido creadas.'
            );
            return false;
          }
          return true;
        } catch (error) {
          Swal.showValidationMessage(`Error: ${String(error)}`);
          return false;
        }
      },
      allowOutsideClick: () => !Swal.isLoading(),
    }).then(result => {
      if (result.isConfirmed) {
        Swal.fire({
          title: '¡Generadas!',
          text: 'Las fases y los encuentros han sido creados exitosamente.',
          icon: 'success',
        }).then(async () => {
          await getStagesByFilters({ divisionId: id });
        });
      }
      onClose();
    });
  }, [id, onClose, generateStagesAutomatically, getStagesByFilters]);

  return null;
};
