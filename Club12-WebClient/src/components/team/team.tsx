import React, { useState } from "react";
import { Box, Button, Grid, Paper, TextField, Typography } from "@mui/material";
import { ITeam } from "../../types/teams/team";

interface TeamProps {
  team: ITeam;
  onUpdate: (team: ITeam) => void;
  onDelete: (id: string) => void;
}

export const Team: React.FC<TeamProps> = ({ team, onUpdate, onDelete }) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editedName, setEditedName] = useState(team.name || "");
  const [editedThreeLetterCode, setEditedThreeLetterCode] = useState(team.threeLetterCode || "");
  const [editedDivisionId, setEditedDivisionId] = useState(team.divisionId || "");

  const handleEditClick = () => {
    setIsEditing(true);
  };

  const handleSaveClick = async () => {
    const updatedTeam: ITeam = {
      ...team,
      name: editedName,
      threeLetterCode: editedThreeLetterCode,
      divisionId: editedDivisionId,
    };

    const response = await fetch(`/api/teams/${team.id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(updatedTeam),
    });

    if (response.ok) {
      onUpdate(updatedTeam);
      setIsEditing(false);
    } else {
      // Manejar errores
    }
  };

  const handleDeleteClick = async () => {
    const response = await fetch(`/api/teams/${team.id}`, {
      method: 'DELETE',
    });

    if (response.ok) {
      onDelete(team.id);
    } else {
      // Manejar errores
    }
  };

  return (
    <Grid item xs={12} display="flex" alignItems="center" justifyContent="center" minHeight="1vh">
      <Paper
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

          {/* THREE LETTER CODE */}
          <Grid item flex="1" xs={3} sx={{ m: "auto" }}>
            {isEditing ? (
              <TextField
                label="Three Letter Code"
                variant="outlined"
                color="secondary"
                fullWidth
                value={editedThreeLetterCode}
                onChange={(e) => setEditedThreeLetterCode(e.target.value)}
              />
            ) : (
              <Typography variant="h4">{editedThreeLetterCode || "no code"}</Typography>
            )}
          </Grid>

          {/* DIVISION ID */}
          <Grid item flex="1" xs={5} sx={{ p: 0, width: "100%", m: "auto" }}>
            {isEditing ? (
              <TextField
                label="Division ID"
                variant="outlined"
                color="secondary"
                fullWidth
                value={editedDivisionId}
                onChange={(e) => setEditedDivisionId(e.target.value)}
              />
            ) : (
              <Typography variant="body1">{editedDivisionId || "no division"}</Typography>
            )}
          </Grid>

          {/* BUTTONS */}
          <Grid item xs={3} sx={{ p: 0, m: "auto" }}>
            {isEditing ? (
              <Button sx={{ m: 0, marginRight: "3px" }} variant="contained" color="success" onClick={handleSaveClick}>
                Save
              </Button>
            ) : (
              <>
                <Button sx={{ m: 0, marginRight: "3px" }} variant="contained" color="secondary" onClick={handleEditClick}>
                  Edit
                </Button>
                <Button sx={{ m: 0 }} variant="contained" color="error" onClick={handleDeleteClick}>
                  Delete
                </Button>
              </>
            )}
          </Grid>
        </Grid>
      </Paper>
    </Grid>
  );
};
