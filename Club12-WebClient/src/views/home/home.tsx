import { useEffect } from "react";
import { useAuth } from "../../modules/auth/hook/useAuth.hook";
import { NavMenu } from "./navMenu";
import { redirect } from "react-router-dom";
import { IndexTournament } from "../tournament";
import { TournamentProvider } from "../../modules/tournament/context/tournament.context";

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
