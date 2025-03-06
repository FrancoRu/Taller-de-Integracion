import * as React from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Typography,
  useTheme,
  Box,
} from "@mui/material";

function createData(player: string, number: number) {
  return { player, number };
}

const rows = [
  createData("Lionel Messi", 3),
  createData("Cristiano Ronaldo", 2),
  createData("Neymar Jr.", 5),
  createData("Kevin De Bruyne", 1),
  createData("Kylian Mbappé", 4),
];

const SanctionsTable: React.FC = () => {
  const theme = useTheme();

  return (
    <Box
      justifyContent="center"
      alignItems="center"
      minHeight="90vh"
      padding={3}
      sx={{ backgroundColor: theme.palette.background.default }}>
      <Typography variant="h5" sx={{ fontWeight: "bold", marginBottom: 2 }}>
        ⚠️ Jugadores Sancionados
      </Typography>

      <TableContainer component={Paper} sx={{ borderRadius: 2, overflow: "hidden" }}>
        <Table sx={{ minWidth: 650 }} aria-label="styled table">
          <TableHead>
            <TableRow sx={{ backgroundColor: theme.palette.primary.light}}>
              <TableCell sx={{ fontWeight: "bold", color: "white" }}>Jugador</TableCell>
              <TableCell align="right" sx={{ fontWeight: "bold", color: "white" }}>Sanciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row, index) => (
              <TableRow
                key={row.player}
                sx={{
                  backgroundColor: index % 2 === 0 ? theme.palette.grey[100] : "white",
                }}
              >
                <TableCell component="th" scope="row" sx={{ fontWeight: 500 }}>
                  {row.player}
                </TableCell>
                <TableCell align="right" sx={{ fontWeight: 500 }}>{row.number}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
};

export default SanctionsTable;
