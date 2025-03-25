import { useState } from "react";
import {  Box,  Typography,  Grid,  TextField,  Button,  Table,  TableBody,  TableCell,  TableHead,  TableRow,  Card,  CardContent,  Paper,  IconButton,  useTheme,} from "@mui/material";
import DeleteIcon from "@mui/icons-material/Delete";

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

  // Función para eliminar un jugador
  const handleRemovePlayer = (id: number) => {
    setPlayers(players.filter((player) => player.id !== id));
  };

  const handleSubmit = async () => {
    if (!teamName || !imageUrl || players.length === 0) {
      alert("Please fill all fields and add at least one player.");
      return;
    }
  
    const formData = new FormData();
    formData.append("Name", teamName);
    formData.append("DivisionId", "1"); 
  
    if (imageUrl) {
      const response = await fetch(imageUrl);
      const blob = await response.blob();
      formData.append("LogoFile", blob, "team-logo.png");
    }
  
    try {
      const response = await fetch("https://tu-api.com/api/teams/", {
        method: "POST",
        body: formData,
      });
  
      if (!response.ok) {
        throw new Error("Failed to create team");
      }
  
      const data = await response.json();
      console.log("Team Created:", data);
      alert("Team created successfully!");
    } catch (error) {
      console.error("Error:", error);
      alert("An error occurred while creating the team.");
    }
  };
  

  return (
    <Paper
      elevation={4}
      sx={{
        mt: 3,
        padding: 3,
        borderRadius: 3,
        backgroundColor: theme.palette.background.paper,
        boxShadow: 3,
      }}
    >
      <Box p={3}>
        <Typography variant="h3" gutterBottom>
          Create a New Team
        </Typography>
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <TextField
              fullWidth
              label="Team Name"
              value={teamName}
              onChange={(e) => setTeamName(e.target.value)}
              margin="normal"
            />
          </Grid>
          <Grid item xs={12} md={4}>
            <input type="file" accept="image/*" onChange={handleImageUpload} style={{ display: "none" }} id="imageUpload" />
            <label htmlFor="imageUpload">
              <Card
                sx={{
                  cursor: "pointer",
                  display: "flex",
                  flexDirection: "column",
                  alignItems: "center",
                  justifyContent: "center",
                  height: 300,
                  width: 500,
                  border: "2px dashed gray",
                  backgroundColor: "#f5f5f5",
                  overflow: "hidden",
                }}
              >
                {imageUrl ? (
                  <img src={imageUrl} alt="Team" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
                ) : (
                  <CardContent>
                    <Typography variant="h5" align="center" sx={{ color: "gray" }}>
                      +
                    </Typography>
                    <Typography variant="h6" align="center" sx={{ color: "gray" }}>
                      Add Image
                    </Typography>
                  </CardContent>
                )}
              </Card>
            </label>
          </Grid>
        </Grid>

        <Box mt={4}>
          <Typography variant="h5" gutterBottom>
            Add Players
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={4}>
              <TextField
                fullWidth
                label="Name"
                value={newPlayer.name}
                onChange={(e) => setNewPlayer({ ...newPlayer, name: e.target.value })}
              />
            </Grid>
            <Grid item xs={4}>
              <TextField
                fullWidth
                label="Position"
                value={newPlayer.position}
                onChange={(e) => setNewPlayer({ ...newPlayer, position: e.target.value })}
              />
            </Grid>
            <Grid item xs={3}>
              <TextField
                fullWidth
                label="Number"
                type="number"
                value={newPlayer.number}
                onChange={(e) => setNewPlayer({ ...newPlayer, number: e.target.value })}
              />
            </Grid>
            <Grid item xs={1}>
              <Button variant="contained" color="primary" onClick={handleAddPlayer}>
                Add
              </Button>
            </Grid>
          </Grid>
        </Box>

        {players.length > 0 && (
          <Box mt={4}>
            <Typography variant="h5" gutterBottom>
              Players
            </Typography>
            <Table>
              <TableHead>
                <TableRow sx={{ backgroundColor: theme.palette.primary.light }}>
                  <TableCell sx={{ fontWeight: "bold", color: theme.palette.primary.contrastText }}>Nombre</TableCell>
                  <TableCell sx={{ fontWeight: "bold", color: theme.palette.primary.contrastText }}>Posicion</TableCell>
                  <TableCell sx={{ fontWeight: "bold", color: theme.palette.primary.contrastText }}>Numero</TableCell>
                  <TableCell sx={{ fontWeight: "bold", color: theme.palette.primary.contrastText, textAlign: "center" }}>Eliminar</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {players.map((player, index) => (
                  <TableRow key={player.id} sx={{ backgroundColor: index % 2 === 0 ? theme.palette.grey[100] : "white" }}>
                    <TableCell>{player.name}</TableCell>
                    <TableCell>{player.position}</TableCell>
                    <TableCell>{player.number}</TableCell>
                    <TableCell sx={{ textAlign: "center" }}>
                      <IconButton color="error" onClick={() => handleRemovePlayer(player.id)}>
                        <DeleteIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        )}

        <Box mt={4}>
          <Button variant="contained" color="secondary" onClick={handleSubmit}>
            Create Team
          </Button>
        </Box>
      </Box>
    </Paper>
  );
};

export default CreateTeam;
