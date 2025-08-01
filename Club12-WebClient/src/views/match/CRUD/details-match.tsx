import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import React, { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { IMatchContextProps, IMatchResponse } from '@/modules/match/type/match';
import { EditIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import {
  Card,
  CardContent,
  Stack,
  Typography,
  Tooltip,
  IconButton,
  Grid,
  Chip,
} from '@mui/material';
import { DeleteMatch } from './delete-match';
import { formatMatchDate } from '@/modules/core/utils/formatDate';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { RenderTeamMatch } from '@/views/team/CRUD/detail-team';
import { MatchStatusChip } from '../util/MatchStatusChip';

export const DetailMatch: React.FC = () => {
  const { id } = useParams<{ id: GUID }>();
  const { match, getMatchById } = useMatch();
  const { setMessage }: IErrorContextProp = useError();
  const navigate = useNavigate();
  if (!id) {
    navigate(RoutesNavigationViews.Home, { replace: true });
    setMessage(400, ['Id no encontrado']);
    return;
  }

  useEffect(() => {
    (async () => {
      await getMatchById(id);
    })();
  }, [match]);

  return <>{match && <RenderMatchDetails {...match} />}</>;
};

const RenderMatchDetails: React.FC<IMatchResponse> = ({
  id,
  matchDate,
  matchType,
  homeTeam,
  visitorTeam,
  isFinished,
  winningTeamId,
  venue,
}) => {
  const navigate = useNavigate();
  const [showPopupDelete, setShowPopupDelete] = useState(false);
  const { deleteMatchById }: IMatchContextProps = useMatch();

  return (
    <Card sx={{ width: '98%', mx: 'auto', px: { xs: 2, sm: 3, md: 4 } }}>
      <CardContent>
        <Grid container spacing={3}>
          {/* Columna 1: Información del partido */}
          <Grid item xs={12} md={4}>
            <Stack spacing={2} alignItems="center">
              <Typography variant="body2" color="text.secondary">
                <strong>Fecha:</strong> {formatMatchDate(matchDate)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                <strong>Tipo de partido:</strong> {matchType}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                <strong>Resultado:</strong> {homeTeam.score} -{' '}
                {visitorTeam.score}
              </Typography>
              {venue && (
                <Typography
                  variant="body2"
                  color="text.secondary"
                  fontWeight="bold"
                >
                  <strong>Cancha:</strong>{' '}
                  <Link
                    to={`/${RoutesNavigationViews.Venue}/${venue.id}`}
                    style={{ textDecoration: 'none', color: 'inherit' }}
                  >
                    {venue.name}
                  </Link>
                </Typography>
              )}
              <MatchStatusChip startTime={matchDate} isFinished={isFinished} />
              <Stack direction="row" spacing={4} alignItems="center">
                <RenderTeamMatch {...homeTeam} />
                <Typography variant="h6">vs</Typography>
                <RenderTeamMatch {...visitorTeam} />
              </Stack>
            </Stack>
          </Grid>

          <Grid item xs={12} md={4} alignContent="center">
            {isFinished && winningTeamId && (
              <Stack direction="column" alignItems="center" spacing={3}>
                <RenderTeamMatch
                  {...(winningTeamId === homeTeam.id ? homeTeam : visitorTeam)}
                />
                <Chip label="Ganador" color="success" size="small" />
              </Stack>
            )}
          </Grid>

          <Grid item xs={12} md={4}>
            <Stack direction="row" spacing={1} justifyContent="flex-end">
              <Tooltip title="Editar partido">
                <IconButton color="primary" onClick={() => navigate(`editar`)}>
                  <EditIcon />
                </IconButton>
              </Tooltip>
              <Tooltip title="Eliminar partido">
                <IconButton
                  color="error"
                  onClick={() => setShowPopupDelete(true)}
                >
                  <DeleteIcon />
                </IconButton>
              </Tooltip>
            </Stack>
          </Grid>
        </Grid>
      </CardContent>

      {showPopupDelete && (
        <DeleteMatch
          id={id}
          fn={deleteMatchById}
          onClose={() => setShowPopupDelete(false)}
        />
      )}
    </Card>
  );
};
