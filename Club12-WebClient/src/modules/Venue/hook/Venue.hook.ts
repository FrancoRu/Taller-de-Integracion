import { useContext } from "react";
import { VenueContext } from "../context/Venue.context";

export const useVenue = () => {
  const context = useContext(VenueContext);
  if (!context) {
    throw new Error("useVenue must be used within a VenueProvider");
  }
  return context;
};
