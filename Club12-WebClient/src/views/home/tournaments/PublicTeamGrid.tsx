import { useNavigate } from 'react-router-dom';
import { Box, Grid, Typography } from '@mui/material';
import { ITeamResponse } from '@/modules/team/type/team.d';
import TeamLogo from '@/views/core/components/TeamLogo';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

interface PublicTeamGridProps {
  teams: ITeamResponse[];
  /** Message shown when the division has no teams yet. */
  emptyLabel?: string;
}

/**
 * A responsive grid of team cards (logo + name + code) that link to each
 * team's public page. Used inside a division's "Equipos" tab so a visitor
 * browsing a zone sees exactly the teams that play in it.
 */
export default function PublicTeamGrid({ teams, emptyLabel }: PublicTeamGridProps) {
  const navigate = useNavigate();

  if (teams.length === 0) {
    return (
      <Typography sx={{ color: 'text.secondary' }}>
        {emptyLabel ?? 'Todavía no hay equipos en esta división.'}
      </Typography>
    );
  }

  return (
    <Grid container spacing={2}>
      {teams.map(team => (
        <Grid key={team.id} size={{ xs: 12, sm: 6, md: 4 }}>
          <Box
            role="button"
            tabIndex={0}
            onClick={() => navigate(APP_ROUTES.publicTeam.build(team.slug ?? team.id))}
            onKeyDown={event => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                navigate(APP_ROUTES.publicTeam.build(team.slug ?? team.id));
              }
            }}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1.5,
              p: 1.5,
              border: '1px solid',
              borderColor: 'divider',
              borderRadius: 1,
              cursor: 'pointer',
              transition: 'border-color 0.15s, background-color 0.15s',
              '&:hover': { bgcolor: 'action.hover', borderColor: 'primary.main' },
            }}
          >
            <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={36} />
            <Box sx={{ minWidth: 0 }}>
              <Typography variant="body2" noWrap sx={{ fontWeight: 500 }}>
                {team.name}
              </Typography>
              <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                {team.threeLetterCode}
              </Typography>
            </Box>
          </Box>
        </Grid>
      ))}
    </Grid>
  );
}
