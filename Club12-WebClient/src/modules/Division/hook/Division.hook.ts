import { useContext } from "react";
import { DivisionContext } from "../context/Division.context";

export const useDivision = () => {
  const context = useContext(DivisionContext);
  if (!context) {
    throw new Error("useDivision must be used within a DivisionProvider");
  }
  return context;
};
