import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IMatchResponse } from '@/modules/match/type/match';
import { useStage } from '@/modules/stage/hook/stage.hook';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Stack,
  Typography,
  Tooltip,
  IconButton,
  Avatar,
  Chip,
  Divider,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import LoadingIndicator from '../core/components/LoadingIndicator';
import {
  ArrowForwardIcon,
  EditIcon,
  DeleteIcon,
} from '../core/MUI/icons/icons';
import { RoutesNavigationViews } from '../core/routes-const';
import { NoMatchesMessage } from './NoMatchMessage';
import { DeleteMatch } from './CRUD/delete-match';

export const MatchDashboard: React.FC = () => {
  const { matches, getMatchByFilter } = useMatch();

  const { stage } = useStage();
  const { id } = useParams<{ id: GUID }>();
  const navigate = useNavigate();

  useEffect(() => {
    if (!stage) {
      navigate('/');
    }
  }, [stage, navigate]);

  if (!stage) return null;

  useEffect(() => {
    (async () => {
      await getMatchByFilter({ stageId: id });
    })();
  }, [id]);

  return (
    <Box>
      {matches ? (
        matches.length > 0 ? (
          <Grid container spacing={3} sx={{ px: 2, py: 3 }}>
            {matches.map(m => (
              <Grid item key={m.id} xs={12} sm={8} md={4}>
                <RenderMatch {...m} />
              </Grid>
            ))}
          </Grid>
        ) : (
          <NoMatchesMessage name={stage.name} />
        )
      ) : (
        <LoadingIndicator />
      )}
    </Box>
  );
};

const formatMatchDate = (iso: string) =>
  new Date(iso).toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  });

export const RenderMatch: React.FC<IMatchResponse> = ({
  id,
  matchDate,
  homeTeamName,
  homeTeamLogoUrl,
  visitorTeamName,
  visitorTeamLogoUrl,
  homeScore,
  visitorScore,
  isFinished,
  winningTeamName,
  venue,
}) => {
  const { deleteMatchById } = useMatch();
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState(false);
  const [isAnimating, setIsAnimating] = useState(false);

  const handleNavigate = () => {
    setIsAnimating(true);
    setTimeout(() => {
      navigate(`/${RoutesNavigationViews.Match}/${id}`);
      setIsAnimating(false);
    }, 300);
  };

  const winner =
    isFinished && winningTeamName
      ? `Ganador: ${winningTeamName}`
      : isFinished
        ? 'Empate'
        : 'Pendiente';

  return (
    <Card
      sx={{
        transition: 'transform 0.2s',
        '&:hover': {
          transform: 'scale(1.02)',
        },
      }}
    >
      <CardContent>
        <Stack spacing={1} alignItems="center">
          <Stack
            direction="row"
            spacing={1}
            alignItems="center"
            justifyContent="center"
            flexWrap="wrap"
          >
            <Chip
              label={isFinished ? 'Finalizado' : 'Programado'}
              size="small"
              color={isFinished ? 'success' : 'warning'}
              variant="outlined"
            />
          </Stack>

          <Typography variant="caption" color="text.secondary" align="center">
            {formatMatchDate(matchDate)} • {venue?.name}
          </Typography>

          <Divider sx={{ my: 1.5, width: '100%' }} />

          {/* Marcador */}
          <Stack
            direction="row"
            spacing={2}
            alignItems="center"
            justifyContent="space-between"
            sx={{ width: '100%' }}
          >
            {/* Local */}
            <TeamBlock
              name={homeTeamName}
              logo={homeTeamLogoUrl}
              align="right"
            />

            {/* Score */}
            <Box>
              <Typography variant="h5" fontWeight={700} align="center">
                {homeScore} - {visitorScore}
              </Typography>
              <Typography
                variant="caption"
                color="text.secondary"
                display="block"
                textAlign="center"
              >
                {winner}
              </Typography>
            </Box>

            {/* Visitante */}
            <TeamBlock
              name={visitorTeamName}
              logo={visitorTeamLogoUrl}
              align="left"
            />
          </Stack>

          {/* Acciones */}
          <Stack direction="row" spacing={1} justifyContent="center" mt={1.5}>
            <Tooltip title="Ver partido">
              <span>
                <IconButton
                  color="primary"
                  disabled={isAnimating}
                  onClick={handleNavigate}
                  sx={{
                    transition: 'transform 0.5s ease',
                    transform: isAnimating
                      ? 'translateX(10px)'
                      : 'translateX(0)',
                  }}
                >
                  <ArrowForwardIcon titleAccess="Ver partido" />
                </IconButton>
              </span>
            </Tooltip>

            <Tooltip title="Editar partido">
              <IconButton
                color="secondary"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Match}/${id}/editar`)
                }
              >
                <EditIcon titleAccess="Editar partido" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar partido">
              <IconButton color="error" onClick={() => setShowPopup(true)}>
                <DeleteIcon titleAccess="Eliminar partido" />
              </IconButton>
            </Tooltip>
          </Stack>

          {showPopup && (
            <DeleteMatch
              id={id as GUID}
              fn={deleteMatchById}
              onClose={() => setShowPopup(false)}
            />
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};

type TeamBlockProps = {
  name: string;
  logo?: string;
  align?: 'left' | 'right';
};

const TeamBlock: React.FC<TeamBlockProps> = ({
  name,
  logo,
  align = 'left',
}) => (
  <Stack
    direction={align === 'left' ? 'row' : 'row-reverse'}
    spacing={1}
    alignItems="center"
    sx={{ maxWidth: '45%' }}
  >
    <Avatar src={logo} alt={name} />
    <Typography
      variant="body1"
      noWrap
      textOverflow="ellipsis"
      textAlign={align === 'left' ? 'left' : 'right'}
    >
      {name}
    </Typography>
  </Stack>
);
