import { useCallback, useState } from 'react';
import { Container, Tab, Tabs, Typography } from '@mui/material';
import { ScorersViewMode } from '@/modules/scorer/type/scorer.d';
import PlayerScorersTab from '@/views/scorer/playerScorersTab';
import TeamScorersTab from '@/views/scorer/teamScorersTab';

export default function PublicScorersPage() {
  const [tab, setTab] = useState<ScorersViewMode>('player');

  const handleTabChange = useCallback(
    (_: React.SyntheticEvent, value: ScorersViewMode) => {
      setTab(value);
    },
    []
  );

  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Typography variant="h4" component="h1" fontWeight="bold" mb={1}>
        Goleadores
      </Typography>
      <Typography variant="body1" color="text.secondary" mb={3}>
        Ranking de puntos por jugador y por equipo.
      </Typography>

      <Tabs
        value={tab}
        onChange={handleTabChange}
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}
      >
        <Tab label="Por jugador" value="player" />
        <Tab label="Por equipo" value="team" />
      </Tabs>

      {tab === 'player' ? <PlayerScorersTab /> : <TeamScorersTab />}
    </Container>
  );
}
