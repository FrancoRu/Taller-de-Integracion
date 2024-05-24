import {colors, styled} from "@mui/material";
import { Link, LinkProps } from "react-router-dom";

const StyledLinkText = styled(Link)<LinkProps>(({ theme }) => ({
    color:"white",
    "&.MuiTypography-root": {
        color: 'black',
        fontSize: "25px",
        letterSpacing: 1.2,
        width: "100%",
        fontWeight: '700',
        transition: "250ms",
        "&:hover":{
            color: theme.palette.primary.main,
            background: 'none'
        },
        [theme.breakpoints.up('md')]: {
            color: 'white',
            fontSize: "16px",
            fontWeight: '500',
            marginInline: '4px',
            letterSpacing: '0.4px'

        },
    },
}))


export default StyledLinkText