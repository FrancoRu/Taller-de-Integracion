import React, { useState } from "react";
import { Box, Button, Grid, Paper, TextField, Typography } from "@mui/material";
import { IPlayer } from "../../types/players/player";

interface PlayerProps {
  player: IPlayer;
  onUpdate: (player: IPlayer) => void;
  onDelete: (id: number) => void;
}

export const Player: React.FC<PlayerProps> = ({ player, onUpdate, onDelete }) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editedName, setEditedName] = useState(player.name || "");
  const [editedLastName, setEditedLastName] = useState(player.lastName || "");
  const [editedHeight, setEditedHeight] = useState(player.height || 0);
  const [editedWeight, setEditedWeight] = useState(player.weight || 0);

  const handleEditClick = () => {
    setIsEditing(true);
  };

  const handleSaveClick = async () => {
    const updatedPlayer: IPlayer = {
      ...player,
      name: editedName,
      lastName: editedLastName,
      height: editedHeight,
      weight: editedWeight,
    };

    const response = await fetch(`/api/players/${player.id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(updatedPlayer),
    });

    if (response.ok) {
      onUpdate(updatedPlayer);
      setIsEditing(false);
    } else {
      // Manejar errores
    }
  };

  const handleDeleteClick = async () => {
    const response = await fetch(`/api/players/${player.id}`, {
      method: 'DELETE',
    });

    if (response.ok) {
      onDelete(player.id);
    } else {
      // Manejar errores
    }
  };

  return (
      <Box
        display="flex" 
        alignItems="center" 
        justifyContent="center" 
        minHeight="1vh"
        sx={{
          p: 2,
          mb: 2,
          bgcolor: "background.paper",
          color: "text.primary",
          maxWidth: "750px",
          width: "100%",
        }}
      >
        <Grid container spacing={2} sx={{ ml: 0 }} display="flex" flexDirection="row">
          {/* NAME */}
          <Grid item flex="1" xs={3} sx={{ m: "auto" }}>
            {isEditing ? (
              <TextField
                label="Name"
                variant="outlined"
                color="secondary"
                fullWidth
                value={editedName}
                onChange={(e) => setEditedName(e.target.value)}
              />
            ) : (
              <Typography variant="h4">{editedName || "no name"}</Typography>
            )}
          </Grid>

          {/* LAST NAME */}
          <Grid item flex="1" xs={3} sx={{ m: "auto" }}>
            {isEditing ? (
              <TextField
                label="LastName"
                variant="outlined"
                color="secondary"
                fullWidth
                value={editedLastName}
                onChange={(e) => setEditedLastName(e.target.value)}
              />
            ) : (
              <Typography variant="h4">{editedLastName || "no lastName"}</Typography>
            )}
          </Grid>

          {/* HEIGHT */}
          <Grid item flex="1" xs={5} sx={{ p: 0, width: "100%", m: "auto" }}>
            {isEditing ? (
              <TextField
                label="Height"
                variant="outlined"
                color="secondary"
                fullWidth
                value={editedHeight}
                onChange={(e) => setEditedHeight(Number(e.target.value))}
              />
            ) : (
              <Typography variant="body1">{editedHeight || "no height"}</Typography>
            )}
          </Grid>

          {/* WEIGHT */}
          <Grid item flex="1" xs={5} sx={{ p: 0, width: "100%", m: "auto" }}>
            {isEditing ? (
              <TextField
                label="Weight"
                variant="outlined"
                color="secondary"
                fullWidth
                value={editedWeight}
                onChange={(e) => setEditedWeight(Number(e.target.value))}
              />
            ) : (
              <Typography variant="body1">{editedWeight || "no weight"}</Typography>
            )}
          </Grid>

          {/* BUTTONS */}
          <Grid item xs={3} sx={{ p: 0, m: "auto" }}>
            {isEditing ? (
              <Button
                sx={{ m: 0, marginRight: "3px" }}
                variant="contained"
                color="success"
                onClick={handleSaveClick}
              >
                Save
              </Button>
            ) : (
              <>
                <Button
                  sx={{ m: 0, marginRight: "3px" }}
                  variant="contained"
                  color="secondary"
                  onClick={handleEditClick}
                >
                  Edit
                </Button>
                <Button
                  sx={{ m: 0 }}
                  variant="contained"
                  color="error"
                  onClick={handleDeleteClick}
                >
                  Delete
                </Button>
              </>
            )}
          </Grid>
        </Grid>
      </Box>
  );
};
