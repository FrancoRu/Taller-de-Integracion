import React, { useState, useEffect } from "react";
import { Box, CircularProgress, Grid, Typography } from "@mui/material";
import { ITeam } from "../../types/teams/team";
import { Team } from "../../components/team/team";
import { TeamAdd } from "../../components/team/teamAdd";

export const Teams = () => {
  const [teamArray, setTeamArray] = useState<ITeam[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchTeams = async () => {
      const response = await fetch('/api/teams');
      if (response.ok) {
        const teams = await response.json();
        setTeamArray(teams);
      } else {
        // Manejar errores
      }
      setLoading(false);
    };

    fetchTeams();
  }, []);

  const handleTeamAdded = (team: ITeam) => {
    setTeamArray([...teamArray, team]);
  };

  const handleTeamUpdate = (updatedTeam: ITeam) => {
    setTeamArray(
      teamArray.map((team) =>
        team.id === updatedTeam.id ? updatedTeam : team
      )
    );
  };

  const handleTeamDelete = (id: string) => {
    setTeamArray(teamArray.filter((team) => team.id !== id));
  };

  return (
    <Grid sx={{ marginTop: 4 }} container spacing={2}>
      <Grid item xs={12}>
        <TeamAdd onTeamAdded={handleTeamAdded} />
      </Grid>

      {loading ? (
        <Grid
          item
          xs={12}
          sx={{ display: "flex", justifyContent: "center", marginTop: 4 }}
        >
          <CircularProgress />
        </Grid>
      ) : teamArray.length > 0 ? (
        teamArray.map((team) => (
          <Grid item xs={12} key={team.id}>
            <Team
              team={team}
              onUpdate={handleTeamUpdate}
              onDelete={handleTeamDelete}
            />
          </Grid>
        ))
      ) : (
        <Grid item xs={12} sx={{ marginTop: 4 }}>
          <Box
            display="flex"
            alignItems="center"
            justifyContent="center"
            minHeight="1vh"
          >
            <Typography>No se encontraron equipos.</Typography>
          </Box>
        </Grid>
      )}
    </Grid>
  );
};
