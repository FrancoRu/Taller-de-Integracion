import React from "react";
import { useParams } from "react-router-dom";
import {
  Box,
  Typography,
  Grid,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Card,
  CardContent,
  CardMedia,
  useTheme,
} from "@mui/material";

const teamsData = [
  {
    id: 1,
    name: "Wolves",
    stats: { gamesPlayed: 20, wins: 15, losses: 5, pointsScored: 1025 },
    image: "https://thumbs.dreamstime.com/b/wolves-mascot-logo-design-team-sports-gaming-348043314.jpg",
    players: [
      { id: 1, name: "Player 1", position: "Guard", number: 12 },
      { id: 2, name: "Player 2", position: "Forward", number: 7 },
      { id: 3, name: "Player 3", position: "Center", number: 15 },
    ],
  },
];

const TeamsDetails: React.FC = () => {
  const theme = useTheme(); // Obtenemos el tema

  const { teamId } = useParams<{ teamId: string }>();
  const team = teamsData.find((t) => t.id === parseInt(teamId || "", 10));

  if (!team) {
    return (
      <Box p={3} textAlign="center">
        <Typography variant="h6" color="error">
          Team not found!
        </Typography>
      </Box>
    );
  }

  return (
    <Box
      p={3}
      sx={{
        backgroundColor: theme.palette.background.default,
        minHeight: "100vh",
      }}
    >
      <Grid container spacing={3} justifyContent="center">
        {/* Tarjeta del equipo */}
        <Grid item xs={12} md={8}>
          <Card
            sx={{
              display: "flex",
              borderRadius: theme.shape.borderRadius,
              boxShadow: theme.shadows[3],
              backgroundColor: theme.palette.background.paper,
            }}
          >
            <CardMedia
              component="img"
              sx={{
                width: 300,
                objectFit: "cover",
                borderRadius: `${theme.shape.borderRadius}px 0 0 ${theme.shape.borderRadius}px`,
              }}
              image={team.image}
              alt={team.name}
            />
            <CardContent sx={{ flex: 1 }}>
              <Typography
                variant="h4"
                sx={{
                  fontFamily: theme.typography.fontFamily,
                  fontWeight: "bold",
                  color: theme.palette.primary.main,
                  mb: 1,
                }}
              >
                {team.name}
              </Typography>
              <Typography variant="body1">📅 Games Played: {team.stats.gamesPlayed}</Typography>
              <Typography variant="body1">✅ Wins: {team.stats.wins}</Typography>
              <Typography variant="body1">❌ Losses: {team.stats.losses}</Typography>
              <Typography variant="body1">🏀 Points Scored: {team.stats.pointsScored}</Typography>
            </CardContent>
          </Card>
        </Grid>

        {/* Tabla de jugadores */}
        <Grid item xs={12} md={8}>
          <Box mt={3}>
            <Typography
              variant="h5"
              gutterBottom
              sx={{ fontWeight: "bold", color: theme.palette.text.primary }}
            >
              Players
            </Typography>
            <TableContainer component={Paper} sx={{ borderRadius: theme.shape.borderRadius, boxShadow: theme.shadows[3] }}>
              <Table>
                <TableHead>
                  <TableRow sx={{ backgroundColor: theme.palette.primary.light }}>
                    <TableCell sx={{ fontWeight: "bold", color: theme.palette.primary.contrastText }}>Name</TableCell>
                    <TableCell sx={{ fontWeight: "bold", color: theme.palette.primary.contrastText }}>Position</TableCell>
                    <TableCell sx={{ fontWeight: "bold", color: theme.palette.primary.contrastText }}>Number</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {team.players.map((player) => (
                    <TableRow key={player.id}>
                      <TableCell>{player.name}</TableCell>
                      <TableCell>{player.position}</TableCell>
                      <TableCell>{player.number}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Box>
        </Grid>
      </Grid>
    </Box>
  );
};

export default TeamsDetails;
