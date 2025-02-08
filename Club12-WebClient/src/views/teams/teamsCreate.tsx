import { useState } from "react";
import {
  Box,
  Typography,
  Grid,
  TextField,
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Card,
  CardContent,
  useTheme,
} from "@mui/material";

const CreateTeam: React.FC = () => {
  const theme = useTheme();
  const [teamName, setTeamName] = useState("");
  const [imageUrl, setImageUrl] = useState<string | null>(null);
  const [players, setPlayers] = useState<{ id: number; name: string; position: string; number: number }[]>([]);
  const [newPlayer, setNewPlayer] = useState({ name: "", position: "", number: "" });

  const handleImageUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files && event.target.files[0]) {
      const file = event.target.files[0];
      const reader = new FileReader();
      reader.onload = () => {
        setImageUrl(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleAddPlayer = () => {
    if (newPlayer.name && newPlayer.position && newPlayer.number) {
      setPlayers([...players, { id: players.length + 1, ...newPlayer, number: Number(newPlayer.number) }]);
      setNewPlayer({ name: "", position: "", number: "" });
    }
  };

  const handleSubmit = () => {
    const newTeam = {
      id: Math.floor(Math.random() * 1000),
      name: teamName,
      image: imageUrl,
      players,
    };
    console.log("Team Created:", newTeam);
  };

  return (
    <Box p={3} sx={{ backgroundColor: theme.palette.background.default, borderRadius: 2, boxShadow: 3 }}>
      <Typography variant="h4" color={theme.palette.primary.main} gutterBottom>
        Create a New Team
      </Typography>
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
           <input
            type="file"
            accept="image/*"
            onChange={handleImageUpload}
            style={{ display: "none" }}
            id="imageUpload"
          />
          <label htmlFor="imageUpload">
            <Card
              sx={{
                cursor: "pointer",
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                height: 300,
                width: 300,
                border: `2px dashed ${theme.palette.grey[400]}`,
                backgroundColor: theme.palette.background.paper,
                overflow: "hidden",
                borderRadius: 4
              }}
            >
              {imageUrl ? (
                <img src={imageUrl} alt="Team" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
              ) : (
                <CardContent>
                  <Typography variant="h5" align="center" color={theme.palette.text.secondary}>
                    +
                  </Typography>
                  <Typography variant="h6" align="center" color={theme.palette.text.secondary}>
                    Add Image
                  </Typography>
                </CardContent>
              )}
            </Card>
          </label>
            <CardContent sx={{ flex: 1 }}>
            <TextField
            fullWidth
            label="Team Name"
            value={teamName}
            onChange={(e) => setTeamName(e.target.value)}
            margin="normal"
            variant="outlined"
          />
              <Typography variant="body1">📅 Games Played: </Typography>
              <Typography variant="body1">✅ Wins: </Typography>
              <Typography variant="body1">❌ Losses: </Typography>
              <Typography variant="body1">🏀 Points Scored: </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>      

      <Box mt={4} p={2} sx={{ backgroundColor: theme.palette.background.paper, borderRadius: 2, boxShadow: 2 }}>
        <Typography variant="h5" color={theme.palette.secondary.main} gutterBottom>
          Add Players
        </Typography>
        <Grid container spacing={2}>
          <Grid item xs={4}>
            <TextField
              fullWidth
              label="Name"
              value={newPlayer.name}
              onChange={(e) => setNewPlayer({ ...newPlayer, name: e.target.value })}
              variant="outlined"
            />
          </Grid>
          <Grid item xs={4}>
            <TextField
              fullWidth
              label="Position"
              value={newPlayer.position}
              onChange={(e) => setNewPlayer({ ...newPlayer, position: e.target.value })}
              variant="outlined"
            />
          </Grid>
          <Grid item xs={3}>
            <TextField
              fullWidth
              label="Number"
              type="number"
              value={newPlayer.number}
              onChange={(e) => setNewPlayer({ ...newPlayer, number: e.target.value })}
              variant="outlined"
            />
          </Grid>
          <Grid item xs={1}>
            <Button variant="contained" color="primary" sx={{ backgroundColor: theme.palette.primary.light}} onClick={handleAddPlayer}>
              Add
            </Button>
          </Grid>
        </Grid>
      </Box>

      {players.length > 0 && (
        <Box mt={4} p={2} sx={{ backgroundColor: theme.palette.background.paper, borderRadius: 2, boxShadow: 2 }}>
          <Typography variant="h5" color={theme.palette.secondary.main} gutterBottom>
            Players
          </Typography>
          <Table>
            <TableHead>
              <TableRow  sx={{ backgroundColor: theme.palette.primary.light }}>
                <TableCell>Name</TableCell>
                <TableCell>Position</TableCell>
                <TableCell>Number</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {players.map((player) => (
                <TableRow key={player.id}>
                  <TableCell>{player.name}</TableCell>
                  <TableCell>{player.position}</TableCell>
                  <TableCell>{player.number}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
      )}

      <Box mt={4} textAlign="center">
        <Button variant="contained" color="secondary" onClick={handleSubmit}>
          Create Team
        </Button>
      </Box>
    </Box>
  );
};

export default CreateTeam;