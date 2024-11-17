import { useContext } from "react";
import { PlayerStatisticContext } from "../context/PlayerStatistic.context";

export const usePlayerStatistic = () => {
  const context = useContext(PlayerStatisticContext);
  if (!context) {
    throw new Error("usePlayerStatistic must be used within a PlayerStatisticProvider");
  }
  return context;
};
