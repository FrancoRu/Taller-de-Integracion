import { useContext } from "react";
import { PlayerStatisticContext } from "../context/playerStatistic.context";

export const usePlayerStatistic = () => {
  const context = useContext(PlayerStatisticContext);
  if (!context) {
    throw new Error(
      "usePlayerStatistic must be used within a PlayerStatisticProvider"
    );
  }
  return context;
};
