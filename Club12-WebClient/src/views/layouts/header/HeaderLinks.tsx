import React from "react";
import Stack  from "@mui/material/Stack";
import StyledLinkText from "../StyledLinkText";


 const HeaderLinks: React.FC = ()=>{

    return(
        <Stack direction="row" alignItems="center" >            
            <Stack direction={"row"} alignItems={"center"} spacing={2}>   
                <StyledLinkText to={"/"}>Inicio</StyledLinkText>
                <StyledLinkText to={"/players"}>Jugadores</StyledLinkText>
                <StyledLinkText to={"/teams"}>Equipos</StyledLinkText>
            </Stack>    
        </Stack>
 )
}
export default HeaderLinks