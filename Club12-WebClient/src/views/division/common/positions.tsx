import { TypeMatch } from '@/modules/match/type/match.d';
import React from 'react';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { DataPositions } from '@/modules/tournament/type/tournament';
import { IDashboardStage } from '@/modules/stage/type/stage.d';

export const Positions: React.FC<IDashboardStage> = ({ stages }) => {
  const regularMatches = stages.flatMap(d =>
    d.matchesByWeek.filter(m => m.type === TypeMatch.Regular)
  );

  const statsByTeam: Record<string, DataPositions> = {};

  regularMatches.forEach(match => {
    const { homeTeamName, visitorTeamName, homeScore, visitorScore } = match;

    if (!statsByTeam[homeTeamName]) {
      statsByTeam[homeTeamName] = {
        id: homeTeamName,
        nameTeam: homeTeamName,
        positions: { pj: 0, pg: 0, pp: 0, gf: 0, gc: 0, dif: 0, pts: 0 },
      };
    }
    if (!statsByTeam[visitorTeamName]) {
      statsByTeam[visitorTeamName] = {
        id: visitorTeamName,
        nameTeam: visitorTeamName,
        positions: { pj: 0, pg: 0, pp: 0, gf: 0, gc: 0, dif: 0, pts: 0 },
      };
    }

    statsByTeam[homeTeamName].positions.pj += 1;
    statsByTeam[visitorTeamName].positions.pj += 1;

    statsByTeam[homeTeamName].positions.gf += homeScore;
    statsByTeam[homeTeamName].positions.gc += visitorScore;

    statsByTeam[visitorTeamName].positions.gf += visitorScore;
    statsByTeam[visitorTeamName].positions.gc += homeScore;

    if (homeScore > visitorScore) {
      statsByTeam[homeTeamName].positions.pg += 1;
      statsByTeam[homeTeamName].positions.pts += 3;
      statsByTeam[visitorTeamName].positions.pp += 1;
    } else if (visitorScore > homeScore) {
      statsByTeam[visitorTeamName].positions.pg += 1;
      statsByTeam[visitorTeamName].positions.pts += 3;
      statsByTeam[homeTeamName].positions.pp += 1;
    } else {
      statsByTeam[homeTeamName].positions.pts += 1;
      statsByTeam[visitorTeamName].positions.pts += 1;
    }
  });

  Object.values(statsByTeam).forEach(team => {
    team.positions.dif = team.positions.gf - team.positions.gc;
  });

  const positionsArray = Object.values(statsByTeam).sort((a, b) => {
    if (b.positions.pts !== a.positions.pts) {
      return b.positions.pts - a.positions.pts;
    }
    return b.positions.dif - a.positions.dif;
  });

  console.log(statsByTeam);
  const columns: GridColDef[] = [
    {
      field: 'nameTeam',
      headerName: 'Equipo',
      flex: 2,
      sortable: false,
      align: 'left',
      headerAlign: 'left',
    },
    {
      field: 'pj',
      headerName: 'PJ',
      flex: 1,
      type: 'number',
      sortable: false,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'pg',
      headerName: 'PG',
      flex: 1,
      type: 'number',
      sortable: false,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'pp',
      headerName: 'PP',
      flex: 1,
      type: 'number',
      sortable: false,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'gf',
      headerName: 'GF',
      flex: 1,
      type: 'number',
      sortable: false,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'gc',
      headerName: 'GC',
      flex: 1,
      type: 'number',
      sortable: false,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'dif',
      headerName: 'DIF',
      flex: 1,
      type: 'number',
      sortable: false,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'pts',
      headerName: 'PTS',
      flex: 1,
      type: 'number',
      sortable: false,
      align: 'center',
      headerAlign: 'center',
    },
  ];

  const rows = positionsArray.map(team => ({
    id: team.id,
    nameTeam: team.nameTeam,
    pj: team.positions.pj,
    pg: team.positions.pg,
    pp: team.positions.pp,
    gf: team.positions.gf,
    gc: team.positions.gc,
    dif: team.positions.dif,
    pts: team.positions.pts,
  }));

  return (
    <>
      <h1>Posiciones</h1>
      <div style={{ width: '100%' }}>
        <DataGrid
          rows={rows}
          columns={columns}
          autoHeight
          disableColumnMenu={true}
          hideFooter
        />
      </div>
    </>
  );
};
