import { useAuth } from "../../hooks/auth/useAuth";

export const Home = () => {
  const { isAuthenticated, user } = useAuth();
  return (
    <>
      <h1>
        Bienbenido culeador de{" "}
        <strong>{isAuthenticated && user?.userName}</strong>
      </h1>
    </>
  );
};
