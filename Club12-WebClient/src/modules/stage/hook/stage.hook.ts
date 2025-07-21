import { useContext } from "react";
import { StageContext } from "../context/stage.context";

export const useStage = () => {
  const context = useContext(StageContext);
  if (!context) {
    throw new Error("useStage must be used within a StageProvider");
  }
  return context;
};
