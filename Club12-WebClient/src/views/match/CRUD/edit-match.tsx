import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import {
  IEditMatch,
  IMatchContextProps,
  IMatchResponse,
} from '@/modules/match/type/match';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { Card, CardContent, Typography } from '@mui/material'; // CircularProgress se importa desde LoadingIndicator
import React, { useEffect, useState } from 'react';
import { NoMatchMessage } from '../message/NoMatchMessage';
import { EditNotStartedMatch } from './edits/edit-not-started-match';
import { EditFinishedMatch } from './edits/edit-finished-match';
import { useParams } from 'react-router-dom';
import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IStageContextProps } from '@/modules/stage/type/stage';
import { useStage } from '@/modules/stage/hook/stage.hook';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';

export const EditMatch: React.FC = () => {
  const { errors }: IErrorContextProp = useError();
  const { matchId: id } = useParams<{ matchId: GUID }>();
  const { match, getMatchById }: IMatchContextProps = useMatch();
  const { stage, getStageById }: IStageContextProps = useStage();
  const [editMatch, setEditMatch] = useState<IEditMatch | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      setIsLoading(true);
      if (id) {
        const matchRes: IMatchResponse | void = await getMatchById(id);
        if (matchRes && !stage) {
          await getStageById(matchRes.stageId);
        }
      }
      setIsLoading(false);
    };

    fetchData();
  }, [id]);

  useEffect(() => {
    if (match && stage) {
      setEditMatch({
        id: match.id,
        homeScore: match.homeTeam?.score ?? 0,
        visitorScore: match.visitorTeam?.score ?? 0,
        matchDate: new Date(match.matchDate),
        isFinished: match.isFinished,
        venue: match.venue ?? null,
        startDate: stage.startDate,
        endDate: stage.endDate,
      });
    }
  }, [match, stage]);

  if (isLoading) {
    return (
      <CustomBox>
        <LoadingIndicator />
      </CustomBox>
    );
  }

  return match ? (
    <>
      {editMatch && (
        <CustomBox>
          <Card>
            <CardContent>
              <Typography variant="h4" gutterBottom align="center">
                Editar Partido
              </Typography>

              {errors && errors.length > 0 && (
                <>
                  {errors.map((e, i) => (
                    <Typography
                      key={i}
                      color="error"
                      variant="body2"
                      align="center"
                      gutterBottom
                    >
                      {e}
                    </Typography>
                  ))}
                </>
              )}
              {match.isFinished ? (
                <EditFinishedMatch {...editMatch} />
              ) : (
                <EditNotStartedMatch {...editMatch} />
              )}
            </CardContent>
          </Card>
        </CustomBox>
      )}
    </>
  ) : (
    <>
      <NoMatchMessage />
    </>
  );
};
