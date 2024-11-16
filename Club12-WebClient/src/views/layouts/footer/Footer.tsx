import React, { forwardRef } from "react";
import {
  Typography,
  Stack,
  styled,
  Button,
  Divider,
  Link as LinkMui,
  keyframes,
  ButtonProps,
} from "@mui/material";
import Box, { BoxProps } from "@mui/material/Box";
import StyledLinkText from "../StyledLinkText";

// Styled footer component
const StyledFooter = styled("footer")(({ theme }) => ({
  width: "100%",
  background: "linear-gradient(180deg, rgba(0, 0, 0, 1) 0%, rgba(255, 168, 0, 1) 100%)", // Black and orange gradient
  paddingTop: theme.spacing(5), // Adjusted padding for better spacing
  position: "absolute",
  bottom: 0,
  left: 0,
}));

// Footer component
const Footer = forwardRef<any, BoxProps>((props, ref) => {
  return (
    <StyledFooter>
      <Box
        sx={{
          display: "flex",
          flexDirection: { xs: "column", sm: "row" },
          alignItems: "start",
          justifyContent: "center", // Center content horizontally
          gap: 4, // Add some spacing between elements
        }}
      >
        <StyledLinkText>Footer Text</StyledLinkText>
      </Box>
    </StyledFooter>
  );
});

Footer.displayName = "Footer";

export default Footer;



//soporte@wearebombo.com
/*
<Stack direction={{xs: 'column', sm:'row'}} justifyContent={{xs: 'center',md:'space-between'}} alignItems={'center'}>
        <Stack direction={{xs: 'column', sm:'row'}} alignItems={{xs: 'center'}} justifyContent={{xs: 'center',md:'space-between'}} gap={8} >
          <LogoO width={24} height={30} alt={'Logo Bombo Community'} title={'Logo Bombo Community'}  />
          <FollowUs />
        </Stack>
        <Typography sx={{color:'white'}} mt={{xs: 4, sm: 0}} ml={{xs: 0, sm: 2}} fontSize={13}>Copyright 2023 © Bombo Clubbig & Community</Typography>
      </Stack>
      */

/*
 {sections.map((section, index) => (
          <Box sx={{ marginY:{ xs:'3vh', sm: '0px'}}} key={index}>
            <Typography variant="h6">{section.title}</Typography>
            {section.items.map((item, idx) => (
              <Typography key={idx}>{item}</Typography>
            ))}
          </Box>
  ))}

const sections = [
  {
    title: 'BOMBO',
    items: ['Sobre BOMBO', 'NFTs', 'Eventos', 'Novedades']
  },
  {
    title: 'AYUDA',
    items: ['Preguntas Frecuentes', 'Soporte', 'Solicitar Reembolso']
  },
  {
    title: 'TRABAJAR CON NOSOTROS',
    items: ['Productoras', 'Envianos tu CV o Empleos']
  }
];
*/
