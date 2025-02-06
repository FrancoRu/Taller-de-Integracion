import { Link } from "react-router-dom";
import { AppBar, Toolbar, Typography, Button, Box, ButtonProps } from "@mui/material";
import { grey } from "@mui/material/colors";
import { styled } from "@mui/material/styles";

// Define the extended type for ButtonProps to support component and to
interface StyledButtonProps extends ButtonProps {
  component?: React.ElementType;
  to?: string;
}

const StyledAppBar = styled(AppBar)(({ theme }) => ({
  backgroundColor: theme.palette.secondary.main,
}));

const StyledButton = styled(Button)<StyledButtonProps>(({ theme }) => ({
  color: "white",
  textDecoration: "none",
  marginRight: theme.spacing(2),
  fontWeight: "bold",
  "&:hover": {
    color: theme.palette.primary.main,
  },
}));

const AuthButton = styled(Button)<StyledButtonProps>(({ theme }) => ({
  color: 'white',
  fontWeight: 'bold',
  background: theme.palette.primary.light,
  '&:hover': {
    backgroundColor: theme.palette.primary.main,
  },
}));

const menuItems = [
  { label: "Inicio", path: "/" },
  { label: "Quienes Somos", path: "/quienes-somos" },
  { label: "Información", path: "/informacion" },
  { label: "Teams", path: "/teams" },
  { label: "Sancionados", path: "/sanciones" },
];

const NavMenu: React.FC<{ isAuthenticated: boolean, onLogout: () => void }> = ({ isAuthenticated, onLogout }) => {
  return (
    <StyledAppBar position="sticky">
      <Toolbar sx={{ justifyContent: "space-between" }}>
        <Typography variant="h6" sx={{ fontWeight: "bold", color: 'white' }}>
          Club12 - Basquetball 🏀
        </Typography>
        
        <Box sx={{ display: "flex" }}>
          {menuItems.map((item) => (
            <StyledButton key={item.path} component={Link} to={item.path}>
              {item.label}
            </StyledButton>
          ))}
        </Box>

        <Box>
          {isAuthenticated ? (
            <>
              <Typography variant="body1" sx={{ color: grey[900], marginRight: 2 }}>
                Bienvenido, authenticated user!
              </Typography>
              <AuthButton onClick={onLogout}>Log Out</AuthButton>
            </>
          ) : (
            <AuthButton component={Link} to="/login">Log In</AuthButton>
          )}
        </Box>
      </Toolbar>
    </StyledAppBar>
  );
};

export default NavMenu;
