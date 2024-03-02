import { Button as MUIButton } from "@mui/material";
import { IButton } from "../../types/buttons/IButton";



export const Button: React.FC<IButton> = (props) => {
  return (
    <MUIButton
      variant={props.variant ?? "contained"}
      type={props.type}
      color={props.color ?? "secondary"}
      onClick={() => props.handler?.()}
      className={props.classname}
    >
      {props.value}
    </MUIButton>
  );
};
