import { AppBar, AppBarProps, Stack, styled } from "@mui/material";
import { forwardRef } from "react";
import { Link } from "react-router-dom";
import HeaderLinks from "./HeaderLinks";
import HeaderLinksMobile from "./HeaderLinksMobile";


const StyledLogo = styled(Link)(()=>({    
    display: 'flex',
    alignItems: 'center',
    width: 'auto',
    marginRight: 'auto',
}))


const StyledAppBar = styled(AppBar)<AppBarProps>(({ theme }) => ({
    "&.MuiAppBar-root": {
        // background: 'transparent',
        width: "100%",
        height: "50px",
        display:"flex",
        flexDirection: "row",
        alignItems:"center",
        justifyContent: 'space-between',
        background: '#000000',
        backdropFilter: 'blur(40px)',
        [theme.breakpoints.up('md')]: {
            height: "100px",
        },
    },
}));
const Header = forwardRef<any, Omit<AppBarProps, "position">>((props, ref) => {
    return (
        <StyledAppBar ref={ref} position="sticky" {...props} >
                
                {/* <StyledLogo href='/'>
                <Stack sx={{ maxWidth:{xs: '120px', sm: '150px'}, mb: '3px' }}>
                    <Logo style={{minWidth: '120px', width: '100%', height: 'auto', }} alt="Bombo-White-Logo" height={75} width={80}/>
                </Stack>
                </StyledLogo> */}
                <Stack sx={{ml: "auto", display: {xs: "none", md: "flex"}}}>
                    <HeaderLinks/>
                </Stack>
                 <Stack direction="row" justifyContent="flex-end" sx={{width: "100%", display: {xs: "flex", md: "none"}}}>
                    <HeaderLinksMobile/>
                </Stack> 
            </StyledAppBar>
    );
})
export default Header;