import React from "react";
import Stack from "@mui/material/Stack";
import MenuItem from "@mui/material/MenuItem";
import { Button, Drawer, DrawerProps, styled } from "@mui/material";
import StyledLinkText from "../StyledLinkText";
import { Link } from "react-router-dom";

const StyledMenuDrawer = styled(Drawer)<DrawerProps & { special?: boolean }>(
  ({ theme }) => ({
    "&.MuiDrawer-root": {
      ".MuiDrawer-paper": {
        width: "100vw",
        background: "orange",
        opacity: "0.75",
        color: "black",
      },
      textAlign: "left",
    },
  })
);

const StyledButton = styled(Button)(() => ({
  fontSize: "18px",
  paddingInline: "33px",
  marginTop: "8px",
  fontWeight: "400",
  marginRight: "16px",
  backgroundColor: "black !important",
}));

const StyledLogo = styled(Link)(() => ({
  display: "flex",
  alignItems: "center",
  width: "auto",
  marginRight: "auto",
}));

const HeaderLinksMobile: React.FC = () => {
  const [open, setOpen] = React.useState(false);

  const toggleMenu = () => {
    setOpen((prevOpen) => !prevOpen); // Toggles the state
  };

  return (
    <>
      <StyledButton onClick={toggleMenu}>
        {open ? "Close" : "Open"} 
      </StyledButton>
      <StyledMenuDrawer
        sx={{ p: 2 }}
        anchor="right"
        open={open}
        onClose={() => setOpen(false)}
      >
        <Stack sx={{ px: 2, py: 1 }} direction="row" justifyContent="flex-end">
          <Stack sx={{ maxWidth: { xs: "100px", md: "162px" } }}>
            {/* Logo */}
          </Stack>
        </Stack>
        <Stack>
          <MenuItem onClick={() => setOpen(false)}> 
            <Stack direction={"column"} alignItems={"center"} spacing={2}>   
                  <StyledLinkText to={"/"}>Inicio</StyledLinkText>
                  <StyledLinkText to={"/players"}>Jugadores</StyledLinkText>
                  <StyledLinkText to={"/teams"}>Equipos</StyledLinkText>
            </Stack>    
          </MenuItem>
        </Stack>
      </StyledMenuDrawer>
    </>
  );
};
export default HeaderLinksMobile;
