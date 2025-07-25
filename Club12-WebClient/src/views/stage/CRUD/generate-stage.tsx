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
      title: 'Ingrese la cantidad de equipos',
      input: 'number',
      inputAttributes: {
        min: '1',
        step: '1',
        autocapitalize: 'off',
        autocorrect: 'off',
        inputmode: 'numeric',
      },
      inputValidator: value => {
        if (!value || Number(value) < 4) {
          return 'Por favor ingrese una cantidad válida mayor a 3';
        }
        return null;
      },
      showCancelButton: true,
      confirmButtonText: 'Generar',
      cancelButtonText: 'Cancelar',
      showLoaderOnConfirm: true,
      preConfirm: async quantityTeamsStr => {
        const quantityTeams = Number(quantityTeamsStr);
        if (isNaN(quantityTeams) || quantityTeams < 4) {
          Swal.showValidationMessage('Cantidad inválida');
          return false;
        }
        try {
          const result = await generateStagesAutomatically(id, quantityTeams);
          if (!result) {
            Swal.showValidationMessage('No se pudieron generar las fases.');
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
          text: 'Las fases fueron generadas exitosamente.',
          icon: 'success',
        }).then(async () => {
          await getStagesByFilters({ divisionId: id });
        });
      }
      onClose();
    });
  }, [id, onClose]);

  return null;
};
