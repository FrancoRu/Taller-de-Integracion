import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import React, { useEffect } from 'react';
import Swal from 'sweetalert2';

export const GenerateMatch: React.FC<{
  id: GUID;
  onClose: () => void;
}> = ({ id, onClose }) => {
  const { generateMatchesAutomatically, getMatchByFilter } = useMatch();
  useEffect(() => {
    Swal.fire({
      title: 'Ingrese la cantidad de partidos',
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
          const result = await generateMatchesAutomatically(id);
          if (!result) {
            Swal.showValidationMessage('No se pudieron generar los partidos.');
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
          text: 'Los partidos fueron generados exitosamente.',
          icon: 'success',
        }).then(async () => {
          await getMatchByFilter({ stageId: id });
        });
      }
      onClose();
    });
  }, [id, onClose]);

  return null;
};
