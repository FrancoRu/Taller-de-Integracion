import React, { useRef } from "react";
import { Box, Typography } from "@mui/material";

import "../../styles/auth/auth.css";

import formDataLogin from "../../data/readJSON.json";
import { Form } from "../../components/form/form";
import { BaseInputJSON } from "../../types/form/form";
import { handleFields } from "../../utils/formUtils";
import { UserLoginRequest } from "../../types/auths/auth";
import { useAuth } from "../../hooks/auth/useAuth";

export const SignIn: React.FC = () => {
  const dataForm: BaseInputJSON[] = formDataLogin.login;
  const { signIn } = useAuth();
  const ref = useRef<HTMLFormElement>(null);
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>): void => {
    const data = handleFields(event);
    const user: UserLoginRequest = {
      userName: data.userName as string,
      password: data.password as string,
    };
    signIn(user);
    ref.current?.reset();
  };

  return (
    <div id="container">
      <div id="blur">
        <Box
          sx={{
            display: "flex",
            flexDirection: "column",
            maxWidth: "300px",
            margin: "auto",
            backgroundColor: "white",
            color: "black",
            padding: "16px",
            borderRadius: "8px",
            marginTop: "3rem",
            boxShadow: "0 2px 4px rgba(0, 0, 0, 0.6)",
          }}
        >
          <Typography variant="h5">Login</Typography>

          <Form
            formRef={ref}
            handleSubmit={handleSubmit}
            data={dataForm}
            value={"Ingresar"}
          />
        </Box>
      </div>
    </div>
  );
};
