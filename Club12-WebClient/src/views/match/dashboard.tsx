import { useMatch } from '@/modules/match/hook/match.hook';
import { IDashboardMatches, IMatchResponse } from '@/modules/match/type/match';
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
  Chip,
  Divider,
} from '@mui/material';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import LoadingIndicator from '../core/components/LoadingIndicator';
import {
  ArrowForwardIcon,
  EditIcon,
  DeleteIcon,
} from '../core/MUI/icons/icons';
import { RoutesNavigationViews } from '../core/routes-const';
import { NoMatchesMessage } from './message/NoMatchesMessage';
import { DeleteMatch } from './CRUD/delete-match';
import { formatMatchDateToString } from '@/modules/core/utils/formatDate';
import { GUID } from '@/modules/core/types/types';

export const MatchDashboard: React.FC<IDashboardMatches> = ({ matches }) => {
  const { stage } = useStage();

  return (
    stage && (
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
    )
  );
};

const TeamBlock: React.FC<{
  name: string;
  logo?: string;
  align: 'left' | 'right';
}> = ({ name, logo, align }) => {
  return (
    <Stack alignItems={align === 'left' ? 'flex-start' : 'flex-end'}>
      {logo && (
        <Box
          sx={{
            width: 48,
            height: 48,
            borderRadius: '50%',
            overflow: 'hidden',
            border: '2px solid orange',
            boxShadow: '0 0 6px rgba(255,165,0,0.5)',
            mb: 0.5,
          }}
        >
          <img
            src={logo}
            alt={`Logo de ${name}`}
            style={{ width: '100%', height: '100%', objectFit: 'cover' }}
          />
        </Box>
      )}
      <Typography variant="body2" fontWeight={500}>
        {name}
      </Typography>
    </Stack>
  );
};

export const RenderMatch: React.FC<IMatchResponse> = ({
  id,
  matchDate,
  matchType,
  homeTeam,
  visitorTeam,
  isFinished,
  winningTeamId,
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
    isFinished && winningTeamId
      ? `Ganador: ${
          winningTeamId === homeTeam.id ? homeTeam.name : visitorTeam.name
        }`
      : isFinished
        ? 'Empate'
        : 'Pendiente';

  return (
    <Card
      sx={{
        backgroundColor: 'background.paper',
        border: '2px solid',
        borderColor: 'primary.main',
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
            justifyContent="center"
            flexWrap="wrap"
          >
            <Chip
              label={isFinished ? 'Finalizado' : 'Programado'}
              size="small"
              color={isFinished ? 'success' : 'warning'}
              variant="outlined"
            />
            <Chip
              label={matchType}
              size="small"
              color="info"
              variant="outlined"
            />
          </Stack>

          <Typography variant="caption" color="text.secondary" align="center">
            {formatMatchDateToString(matchDate)} • {venue?.name}
          </Typography>

          <Divider sx={{ my: 1.5, width: '100%' }} />

          <Stack
            direction="row"
            spacing={2}
            alignItems="center"
            justifyContent="space-between"
            sx={{ width: '100%' }}
          >
            <TeamBlock
              name={homeTeam.name}
              logo={homeTeam.logoUrl}
              align="right"
            />

            <Box>
              <Typography variant="h5" fontWeight={700} align="center">
                {homeTeam.score} - {visitorTeam.score}
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

            <TeamBlock
              name={visitorTeam.name}
              logo={visitorTeam.logoUrl}
              align="left"
            />
          </Stack>

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
                  <ArrowForwardIcon />
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
                <EditIcon />
              </IconButton>
            </Tooltip>
            {!isFinished && (
              <Tooltip title="Eliminar partido">
                <IconButton color="error" onClick={() => setShowPopup(true)}>
                  <DeleteIcon />
                </IconButton>
              </Tooltip>
            )}
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
