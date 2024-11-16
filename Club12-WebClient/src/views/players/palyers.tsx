import React, { useState, useEffect } from "react";
import { Box, CircularProgress, Grid, Typography } from "@mui/material";
import { Player } from "../../components/player/player";
import { PlayerAdd } from "../../components/player/playerAdd";
import { IPlayer, IBasePlayer } from "../../types/players/player";

export const Players = () => {
  const [playerArray, setPlayerArray] = useState<IPlayer[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchPlayers = async () => {
      const response = await fetch('/api/players');
      if (response.ok) {
        const players = await response.json();
        setPlayerArray(players);
      } else {
        // Manejar errores
      }
      setLoading(false);
    };

    fetchPlayers();
  }, []);

  const handlePlayerAdded = (player: IPlayer) => {
    setPlayerArray([...playerArray, player]);
  };

  const handlePlayerUpdate = (updatedPlayer: IPlayer) => {
    setPlayerArray(
      playerArray.map((player) =>
        player.id === updatedPlayer.id ? updatedPlayer : player
      )
    );
  };

  const handlePlayerDelete = (id: string) => {
    setPlayerArray(playerArray.filter((player) => player.id !== id));
  };

  return (
    <Grid sx={{ marginTop: 4 }} container spacing={2}>
      <Grid item xs={12}>
        <PlayerAdd onPlayerAdded={handlePlayerAdded} />
      </Grid>

      {loading ? (
        <Grid
          item
          xs={12}
          sx={{ display: "flex", justifyContent: "center", marginTop: 4 }}
        >
          <CircularProgress />
        </Grid>
      ) : playerArray.length > 0 ? (
        playerArray.map((player) => (
          <Grid item xs={12} key={player.id}>
            <Player
              player={player}
              onUpdate={handlePlayerUpdate}
              onDelete={handlePlayerDelete}
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
            <Typography>No se encontraron jugadores.</Typography>
          </Box>
        </Grid>
      )}
    </Grid>
  );
};
