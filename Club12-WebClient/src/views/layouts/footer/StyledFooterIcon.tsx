import {styled} from "@mui/material";
import Icon, {IconProps} from "@mui/material/Icon";

const StyledFooterIcon = styled(Icon)<IconProps>(({theme}) => ({
    "&.MuiIcon-root": {
        display:"flex",
        color: theme.palette.primary.light,
        fontSize: "24px",
        transition: "0.5s",
        "&:hover" :{
            color: "#e5dbe7"
        }
    },
}))
export default StyledFooterIcon