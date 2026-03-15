import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import {
  IEditMatch,
  IMatchContextProps,
  IMatchResponse,
  IPutMatchRequest,
} from '@/modules/match/type/match';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { Button } from '@mui/material';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import { MatchForm } from '../match-form';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';

export const EditNotStartedMatch: React.FC<IEditMatch> = ({
  startDate,
  endDate,
}) => {
  const navigate = useNavigate();

  dayjs.extend(utc);
  dayjs.extend(timezone);

  const { match, putMatchByMatchId }: IMatchContextProps = useMatch();

  if (!match || !match.id) navigate(`/${RoutesNavigationViews.Match}`);

  const { errors }: IErrorContextProp = useError();
  const [form, setForm] = useState<IPutMatchRequest>({
    ...match,
    matchDate: dayjs.utc(match?.matchDate).format('YYYY-MM-DDTHH:mm'),
    homeTeamid: match?.homeTeam?.id,
    visitorTeamid: match?.visitorTeam?.id,
    venueid: match?.venue?.id,
  });
  console.log(form);
  const handleUpdate = async () => {
    const messages: string[] = [];
    !form.matchDate && messages.push('La fecha del partido es obligatoria.');

    form.matchDate &&
      (new Date(form.matchDate) < new Date(startDate) ||
        new Date(form.matchDate) > new Date(endDate)) &&
      messages.push(
        'La fecha del partido debe estar dentro del rango de la etapa.'
      );

    const res: IMatchResponse | void = await putMatchByMatchId(
      match?.id as GUID,
      form
    );

    if (res) navigate(`/${RoutesNavigationViews.Match}/${match?.id}`);
  };

  return (
    <>
      <MatchForm
        stageId={match?.stageId as GUID}
        form={form}
        setForm={setForm}
        showTeams={match?.matchType == MatchType.Regular}
        startDate={startDate}
        endDate={endDate}
        errors={errors}
      />
      <Button
        fullWidth
        variant="contained"
        color="primary"
        onClick={handleUpdate}
        sx={{ mt: 2 }}
      >
        Editar Partido
      </Button>
    </>
  );
};
