import React from "react";
import { useNavigate } from "react-router-dom";
import { Grid, Card, CardContent, Typography, CardMedia, Box, useTheme } from "@mui/material";

const teams = [
  { id: 1, name: "Wolves", image: "https://thumbs.dreamstime.com/b/wolves-mascot-logo-design-team-sports-gaming-348043314.jpg" },
  { id: 2, name: "Lions", image: "https://img.freepik.com/vecteurs-premium/logo-sport-lion_27088-255.jpg" },
  { id: 3, name: "Eagles", image: "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSRinm8KyFuNSQ1Ce2RSYdwGAjnnWrkrew9kw&s" },
];

const TeamsGrid: React.FC = () => {
  const navigate = useNavigate();
  const theme = useTheme(); // Usamos el tema

  const handleTeamClick = (id: number) => {
    navigate(`/teams/${id}`);
  };

  return (
    <Box
      sx={{
        minHeight: "100vh",
        backgroundColor: theme.palette.background.default,
        padding: 3,
      }}
    >
      <Grid container spacing={3}>
        {/* Tarjeta "Add New Team" */}
        <Grid item xs={12} sm={6} md={4}>
          <Card
            onClick={() => navigate("/teams/create")}
            sx={{
              cursor: "pointer",
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              borderRadius: theme.shape.borderRadius,
              justifyContent: "center",
              height: 210,
              border: `2px dashed ${theme.palette.primary.light}`,
              backgroundColor: 'white',
              transition: "0.3s",
              "&:hover": {
                backgroundColor: theme.palette.primary.light,
                boxShadow: theme.shadows[5],
              },
            }}
          >
            <CardContent>
              <Typography variant="h4" align="center" sx={{ color: theme.palette.primary.main }}>
                +
              </Typography>
              <Typography variant="h6" align="center" sx={{ color: theme.palette.text.primary }}>
                Add New Team
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        {/* Tarjetas de equipos */}
        {teams.map((team) => (
          <Grid item xs={12} sm={6} md={4} key={team.id}>
            <Card
              onClick={() => handleTeamClick(team.id)}
              sx={{
                cursor: "pointer",
                borderRadius: theme.shape.borderRadius,
                boxShadow: theme.shadows[3],
                transition: "0.3s",
                background: 'white',
                "&:hover": {
                  boxShadow: theme.shadows[6],
                  transform: "scale(1.03)",
                },
              }}
            >
              <CardMedia
                component="img"
                height="140"
                image={team.image}
                alt={team.name}
                sx={{
                  borderTopLeftRadius: theme.shape.borderRadius,
                  borderTopRightRadius: theme.shape.borderRadius,
                }}
              />
              <CardContent>
                <Typography
                  variant="h6"
                  align="center"
                  sx={{ fontWeight: "bold", color: theme.palette.text.primary }}
                >
                  {team.name}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default TeamsGrid;
