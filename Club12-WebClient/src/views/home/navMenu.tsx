import { Link, useLocation } from "react-router-dom";
import { AppBar, Toolbar, Typography, Button, Box, ButtonProps } from "@mui/material";
import { grey } from "@mui/material/colors";
import { styled } from "@mui/material/styles";
import { FaHome, FaUsers, FaInfoCircle, FaBan, FaTrophy } from "react-icons/fa"; //  ICONOS https://react-icons.github.io/react-icons/
import { FaPeopleGroup } from "react-icons/fa6";

// Extender ButtonProps para soportar `active`
interface StyledButtonProps extends ButtonProps {
  component?: React.ElementType;
  to?: string;
  active?: boolean;
}

const StyledAppBar = styled(AppBar)(({ theme }) => ({
  backgroundColor: theme.palette.secondary.main,
}));

const StyledButton = styled(Button)<StyledButtonProps>(({ theme, active }) => ({
  color: active ? theme.palette.primary.main : "white",
  textDecoration: "none",
  fontWeight: "bold",
  borderRadius: theme.shape.borderRadius,
  marginRight: theme.spacing(2),
  "&:hover": {
    color: theme.palette.primary.main,
  },
}));

const AuthButton = styled(Button)<StyledButtonProps>(({ theme }) => ({
  color: "white",
  fontWeight: "bold",
  background: theme.palette.primary.light,
  "&:hover": {
    backgroundColor: theme.palette.primary.main,
  },
}));

const menuItems = [
  { label: "Inicio", path: "/", icon: <FaHome /> },
  { label: "Quienes Somos", path: "/quienes-somos", icon: <FaUsers /> },
  { label: "Información", path: "/informacion", icon: <FaInfoCircle /> },
  { label: "Teams", path: "/teams", icon: <FaPeopleGroup /> },
  { label: "Sancionados", path: "/sanciones", icon: <FaBan /> },
  { label: "Braket", path: "/braket", icon: <FaTrophy /> },
];

const NavMenu: React.FC<{ isAuthenticated: boolean; onLogout: () => void }> = ({
  isAuthenticated,
  onLogout,
}) => {
  const location = useLocation(); // Obtener la ruta actual

  return (
    <StyledAppBar position="sticky">
      <Toolbar sx={{ justifyContent: "space-between" }}>
        <Typography variant="h6" sx={{ fontWeight: "bold", color: "white" }}>
          Club12 - Basquetball 🏀
        </Typography>

        <Box sx={{ display: "flex" }}>
        {menuItems.map((item) => (
          <StyledButton
            key={item.path}
            component={Link}
            to={item.path}
            startIcon={item.icon}
            active={item.path === "/" ? location.pathname === "/" : location.pathname.startsWith(item.path)}
          >
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
