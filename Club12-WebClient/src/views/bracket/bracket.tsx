import React from "react";
import { Bracket } from "react-brackets";
import { Box, Paper, Typography, useTheme } from "@mui/material";

const rounds = [
  {
    title: "Round One",
    seeds: [
      { id: 1, date: new Date().toDateString(), teams: [{ name: "Team A" }, { name: "Team B" }] },
      { id: 2, date: new Date().toDateString(), teams: [{ name: "Team C" }, { name: "Team D" }] },
      { id: 3, date: new Date().toDateString(), teams: [{ name: "Team E" }, { name: "Team F" }] },
      { id: 4, date: new Date().toDateString(), teams: [{ name: "Team G" }, { name: "Team H" }] },
    ],
  },
  {
    title: "Semi Finals",
    seeds: [
      { id: 9, date: new Date().toDateString(), teams: [{ name: "Team A" }, { name: "Team D" }] },
      { id: 10, date: new Date().toDateString(), teams: [{ name: "Team F" }, { name: "Team G" }] },
    ],
  },
  {
    title: "Finals",
    seeds: [
      { id: 10, date: new Date().toDateString(), teams: [{ name: "Team A" }, { name: "Team G" }] },
    ],
  },
];


const Bracket1: React.FC = () => {
  const theme = useTheme();

  return (
    <Box
      sx={{
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        minHeight: "80vh",
        backgroundColor: theme.palette.background.default,
        padding: 3,
      }}
    >
      <Paper
        elevation={4}
        sx={{
          padding: 3,
          borderRadius: 3,
          backgroundColor: theme.palette.background.paper,
          boxShadow: 3,
        }}
      >
        <Typography
          variant="h4"
          sx={{
            fontWeight: "bold",
            textAlign: "center",
            marginBottom: 2,
            color: theme.palette.primary.main,
          }}
        >
          🏆 Tournament Bracket
        </Typography>
        <Bracket rounds={rounds} />
      </Paper>
    </Box>
  );
};

export default Bracket1;


// import { Bracket, IRoundProps, Seed, SeedItem, SeedTeam, IRenderSeedProps } from 'react-brackets';
// import React from 'react';

// const CustomSeed = ({seed, breakpoint, roundIndex, seedIndex, isMiddleOfTwoSided}: RenderSeedProps) => {
//   // breakpoint passed to Bracket component
//   // to check if mobile view is triggered or not

//   // mobileBreakpoint is required to be passed down to a seed
//   const Wrapper = isMiddleOfTwoSided ? SingleLineSeed : Seed
//   return (
//     <Wrapper mobileBreakpoint={breakpoint} style={{ fontSize: 12 }}>
//       <SeedItem>
//         <div>
//           <SeedTeam style={{ color: 'red' }}>{seed.teams[0]?.name || 'NO TEAM '}</SeedTeam>
//           <SeedTeam>{seed.teams[1]?.name || 'NO TEAM '}</SeedTeam>
//         </div>
//       </SeedItem>
//     </Wrapper>
//   );
// };

// const Component = () => {
//   //....
//   return <Bracket rounds={rounds} renderSeedComponent={CustomSeed} twoSided={true} />;
// };




//https://www.npmjs.com/package/@g-loot/react-tournament-brackets
// import { SingleEliminationBracket, DoubleEliminationBracket, Match, SVGViewer } from '@Shenato/react-tournament-brackets';
// import { useWindowSize } from '@uidotdev/usehooks';

// Match data (adjust it based on your actual data structure)
// const matches = [
//   {
//     id: 260005,
//     name: "Final - Match",
//     nextMatchId: null,
//     tournamentRoundText: "4",
//     startTime: "2021-05-30",
//     state: "DONE",
//     participants: [
//       {
//         id: "c016cb2a-fdd9-4c40-a81f-0cc6bdf4b9cc",
//         resultText: "WON",
//         isWinner: false,
//         status: null,
//         name: "giacomo123",
//       },
//       {
//         id: "9ea9ce1a-4794-4553-856c-9a3620c0531b",
//         resultText: null,
//         isWinner: true,
//         status: null,
//         name: "Ant",
//       },
//     ],
//   },
//   Add other matches here...
// ];

// export const DoubleElimination = () => (
//   <DoubleEliminationBracket
//     matches={matches}
//     matchComponent={Match}
//     svgWrapper={({ children, ...props }) => (
//       <SVGViewer width={500} height={500} {...props}>
//         {children}
//       </SVGViewer>
//     )}
//   />
// );
// export const SingleElimination = () => (
//   <SingleEliminationBracket
//     matches={matches}
//     matchComponent={Match}
//     svgWrapper={({ children, ...props }) => (
//       <SVGViewer width={500} height={500} {...props}>
//         {children}
//       </SVGViewer>
//     )}
//   />
// );

