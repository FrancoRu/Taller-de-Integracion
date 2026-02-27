import { GUID } from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IAddMatchRequest } from '@/modules/match/type/match';
import { TypeMatch } from '@/modules/core/enum/match/typeMatch';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { GUID_EMPTY } from '@/views/core/constants/const';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import { Card, CardContent, Typography, Button } from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { MatchForm } from './match-form';
export const CreateMatch: React.FC = () => {
  const { addMatch } = useMatch();

  const { stage } = useStage();
  const navigate = useNavigate();
  const { errors, setMessage } = useError();

  useEffect(() => {
    if (!stage) {
      navigate(`/${RoutesNavigationViews.Home}`, { replace: true });
      setMessage(400, [
        'Hubo un problema al cargar la información de creación de partido. Por favor, inténtelo más tarde.',
      ]);
    }
  }, [stage, navigate, setMessage]);

  const [form, setForm] = useState<IAddMatchRequest>({
    matchDate: '',
    type: TypeMatch.Regular,
    homeTeamid: GUID_EMPTY,
    visitorTeamid: GUID_EMPTY,
    stageId: stage?.id as GUID,
    venueid: GUID_EMPTY,
  });

  const handleCreate = async () => {
    if (!form.matchDate) {
      setMessage(400, ['La fecha del partido es obligatoria.']);
      return;
    }
    if (form.homeTeamid === form.visitorTeamid) {
      setMessage(400, ['Los equipos no pueden ser el mismo.']);
      return;
    }

    const matchDate = new Date(form.matchDate);
    if (
      matchDate < new Date(stage!.startDate) ||
      matchDate > new Date(stage!.endDate)
    ) {
      setMessage(400, [
        'La fecha del partido debe estar dentro del rango de la etapa.',
      ]);
      return;
    }

    const res = await addMatch(form);
    if (res) navigate(`/${RoutesNavigationViews.Match}`);
  };

  return stage ? (
    <CustomBox>
      <Card>
        <CardContent>
          <Typography variant="h4" gutterBottom align="center">
            Crear Partido
          </Typography>

          <MatchForm
            showTeams={true}
            stageId={stage.id}
            startDate={stage.startDate}
            endDate={stage.endDate}
            errors={errors}
            form={form}
            setForm={setForm}
          />

          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleCreate}
            sx={{ mt: 2 }}
          >
            Crear Partido
          </Button>
        </CardContent>
      </Card>
    </CustomBox>
  ) : (
    <Typography>Cargando etapa...</Typography>
  );
};
