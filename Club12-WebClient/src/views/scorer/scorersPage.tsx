import { useCallback, useState } from 'react';
import { Card, CardContent, Tab, Tabs, Typography } from '@mui/material';
import { ScorersViewMode } from '@/modules/scorer/type/scorer.d';
import PlayerScorersTab from '@/views/scorer/playerScorersTab';
import TeamScorersTab from '@/views/scorer/teamScorersTab';

const ScorersPage: React.FC = () => {
  const [tab, setTab] = useState<ScorersViewMode>('team');

  const handleTabChange = useCallback(
    (_: React.SyntheticEvent, value: ScorersViewMode) => {
      setTab(value);
    },
    []
  );

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" sx={{
          mb: 2
        }}>
          Puntuaciones
        </Typography>

        <Tabs
          value={tab}
          onChange={handleTabChange}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Por equipo" value="team" />
          <Tab label="Por jugador" value="player" />
        </Tabs>

        {tab === 'team' ? <TeamScorersTab /> : <PlayerScorersTab />}
      </CardContent>
    </Card>
  );
};

export default ScorersPage;
