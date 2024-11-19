import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../modules/auth/hook/auth.hook";
import { TextField, Button, Container, Typography, Box } from "@mui/material";
import { orange, grey } from "@mui/material/colors";

const Login = () => {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const isAuthenticated = await signIn({ username, password });

    if (isAuthenticated) navigate("/");
  };

  return (
    <Container component="main" maxWidth="xs">
      <Box
        sx={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          padding: 3,
          borderRadius: 2,
          boxShadow: 3,
          backgroundColor: grey[800],
        }}
      >
        <Typography variant="h5" component="h1" gutterBottom sx={{ color: orange[500] }}>
          Club 12 - Admin Sign In
        </Typography>
        <form onSubmit={handleSubmit} style={{ width: "100%" }}>
          <TextField
            variant="outlined"
            margin="normal"
            required
            fullWidth
            label="Username"
            autoFocus
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            sx={{
              backgroundColor: grey[900],
              color: "white",
              "& .MuiInputLabel-root": {
                color: "white",
              },
              "& .MuiOutlinedInput-root": {
                "& fieldset": {
                  borderColor: orange[500],
                },
                "&:hover fieldset": {
                  borderColor: orange[500],
                },
              },
            }}
          />
          <TextField
            variant="outlined"
            margin="normal"
            required
            fullWidth
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            sx={{
              backgroundColor: grey[900],
              color: "white",
              "& .MuiInputLabel-root": {
                color: "white",
              },
              "& .MuiOutlinedInput-root": {
                "& fieldset": {
                  borderColor: orange[500],
                },
                "&:hover fieldset": {
                  borderColor: orange[500],
                },
              },
            }}
          />
          <Button
            type="submit"
            fullWidth
            variant="contained"
            color="warning"
            sx={{
              marginTop: 2,
              backgroundColor: orange[500],
              "&:hover": {
                backgroundColor: orange[600],
              },
            }}
          >
            Sign In
          </Button>
        </form>
      </Box>
    </Container>
  );
};

export default Login;
