import { useEffect } from "react";
import { redirect } from "react-router-dom";
import { useAuth } from "../../modules/auth/hook/auth.hook";
import { TournamentProvider } from "../../modules/tournament/context/tournament.context";
import { IndexTournament } from "../tournament";
import { NavMenu } from "./navMenu";

export const Home = () => {
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    redirect("/");
  }, [isAuthenticated]);
  return (
    <>
      <NavMenu isAuthenticated={isAuthenticated} />
      <h1>{isAuthenticated ? "Autenticado" : "No autenticado"}</h1>
      <TournamentProvider>
        <IndexTournament />
      </TournamentProvider>
    </>
  );
};
