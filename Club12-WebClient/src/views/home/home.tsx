import { useAuth } from "../../hooks/auth/useAuth";

export const Home = () => {
  const { isAuthenticated, user } = useAuth();
  return (
    <>
      <h1>
        Bienvenido {" "}
        <strong>{isAuthenticated && user?.userName}</strong>
      </h1>
    </>
  );
};
