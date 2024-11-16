import {styled} from "@mui/material";
import Box, {BoxProps} from "@mui/material/Box";


const StyledFooterSection = styled(Box)<BoxProps>(({ theme }) => ({
    '&.MuiBox-root': {
        color: "#fff",
        display: "flex",
        flexDirection: "column",
        a: {
            textDecoration: "none"
        },
        width: "80%",
        margin:'auto',

        [theme.breakpoints.down('md')]: {
            alignItems: "center",
            textAlign: "center"
        },
        [theme.breakpoints.down('lg')]: {
            width: '90%',
        },
    },
}));
export default StyledFooterSection