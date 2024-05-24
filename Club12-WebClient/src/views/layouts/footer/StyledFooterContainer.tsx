import {styled} from "@mui/material";
import Box, {BoxProps} from "@mui/material/Box";

const StyledFooterContainer = styled(Box)<BoxProps>(({ theme }) => ({
    "&.MuiBox-root": {
        //background: theme.palette.text.secondary,
        background: 'white',
        minHeight: "250px",
        //position: 'fixed',
        bottom: 0,
        width: '100%',
        backgroundColor: 'black',
        padding: '32px 48px',
    },
}))
export default StyledFooterContainer