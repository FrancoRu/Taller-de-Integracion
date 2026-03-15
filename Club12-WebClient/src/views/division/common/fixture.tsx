import React, { useMemo, useState } from 'react';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import Slide from '@mui/material/Slide';
import { IMatchResponse } from '@/modules/match/type/match.d';
import dayjs from 'dayjs';
import { IDashboardStage, IStageResponse } from '@/modules/stage/type/stage.d';
import { GUID } from '@/modules/core/types/types';

interface StageWithMatches extends IStageResponse {
  matchesByWeek?: IMatchResponse[];
}

interface FixtureRow {
  id: string;
  matchDate: string;
  venueName: string;
  homeTeam: IMatchResponse['homeTeam'];
  visitorTeam: IMatchResponse['visitorTeam'];
}

const TeamCell: React.FC<{
  id?: GUID;
  name: string;
  logoUrl: string;
  score?: number;
}> = ({ id, name, logoUrl, score }) => {
  if (!name) return null;
  return (
    <div
      id={id ?? ''}
      style={{ display: 'flex', alignItems: 'center', gap: 8 }}
      aria-label={`Equipo ${name}`}
    >
      <img
        src={logoUrl}
        alt={name}
        style={{ width: 24, height: 24, objectFit: 'contain' }}
      />
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <strong>{name}</strong>
        {score !== undefined && (
          <span style={{ color: '#aaa', fontWeight: 'bold' }}>{score}</span>
        )}
      </div>
    </div>
  );
};

const PaginationButtons: React.FC<{
  count: number;
  currentPage: number;
  onPageChange: (page: number) => void;
}> = ({ count, currentPage, onPageChange }) => (
  <div
    style={{
      display: 'flex',
      gap: 12,
      marginTop: 20,
      justifyContent: 'center',
    }}
    role="navigation"
    aria-label="Navegación de divisiones"
  >
    {Array.from({ length: count }).map((_, idx) => (
      <button
        key={idx}
        onClick={() => onPageChange(idx)}
        style={{
          width: 32,
          height: 32,
          borderRadius: '50%',
          backgroundColor: idx === currentPage ? '#1e1e1e' : '#2c2c2c',
          color: idx === currentPage ? 'white' : '#aaa',
          border: '1px solid #444',
          cursor: 'pointer',
          fontWeight: idx === currentPage ? 'bold' : 'normal',
          transition: 'all 0.2s',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 0,
        }}
        aria-current={idx === currentPage ? 'page' : undefined}
        aria-label={`Página ${idx + 1}`}
        type="button"
      >
        {idx + 1}
      </button>
    ))}
  </div>
);

export const Fixture: React.FC<IDashboardStage> = ({ stages }) => {
  const stagesWithMatches = stages as StageWithMatches[];
  const [currentPage, setCurrentPage] = useState(0);
  const [slideDirection, setSlideDirection] = useState<'left' | 'right'>(
    'left'
  );

  const currentDivision = useMemo(() => {
    if (!stagesWithMatches || stagesWithMatches.length === 0) return null;
    const index = stagesWithMatches.length - 1 - currentPage;
    return stagesWithMatches[index] ?? null;
  }, [currentPage, stagesWithMatches]);

  const maxPageSize = useMemo(() => {
    if (!stagesWithMatches) return 0;
    return Math.max(
      ...stagesWithMatches.map(d => d.matchesByWeek?.length ?? 0),
      0
    );
  }, [stagesWithMatches]);

  const paddedRows = useMemo(() => {
    if (!currentDivision) return [] as FixtureRow[];

    const matches = currentDivision.matchesByWeek ?? [];
    const mappedMatches: FixtureRow[] = matches.map(match => ({
      id: match.id,
      matchDate: match.matchDate,
      venueName: match.venue?.name ?? '',
      homeTeam: match.homeTeam,
      visitorTeam: match.visitorTeam,
    }));

    const emptyCount = Math.max(maxPageSize - mappedMatches.length, 0);

    const filler: FixtureRow[] = Array.from({ length: emptyCount }, (_, i) => ({
      id: `empty-empty-empty-empty-${i}`,
      matchDate: '',
      venueName: '',
      homeTeam: null,
      visitorTeam: null,
    }));

    return [...mappedMatches, ...filler];
  }, [currentDivision, maxPageSize]);

  const columns: GridColDef<FixtureRow>[] = useMemo(
    () => [
      {
        field: 'date',
        headerName: 'Horario',
        flex: 1,
        valueGetter: params => {
          const date = dayjs(params.row.matchDate);
          return date.isValid() ? date.format('DD/MM/YY') : '';
        },
        sortable: false,
      },
      {
        field: 'venue',
        headerName: 'Cancha',
        flex: 1,
        valueGetter: params => params.row.venueName,
        sortable: false,
      },
      {
        field: 'HomeTeam',
        headerName: 'Equipo 1',
        flex: 1,
        sortable: false,
        renderCell: params => (
          <TeamCell
            id={params.row.homeTeam?.id}
            name={params.row.homeTeam?.name ?? ''}
            logoUrl={params.row.homeTeam?.logoUrl ?? ''}
            score={params.row.homeTeam?.score}
          />
        ),
      },
      {
        field: 'VisitorTeam',
        headerName: 'Equipo 2',
        flex: 1,
        sortable: false,
        renderCell: params => (
          <TeamCell
            id={params.row.visitorTeam?.id}
            name={params.row.visitorTeam?.name ?? ''}
            logoUrl={params.row.visitorTeam?.logoUrl ?? ''}
            score={params.row.visitorTeam?.score}
          />
        ),
      },
    ],
    []
  );

  const handlePageChange = (page: number) => {
    setSlideDirection(page > currentPage ? 'left' : 'right');
    setCurrentPage(page);
  };

  if (!currentDivision) {
    return <p>No hay divisiones disponibles.</p>;
  }

  return (
    <>
      <h1>Fixture</h1>
      <h2 style={{ marginBottom: 16 }}>{currentDivision.name.toUpperCase()}</h2>

      <Slide
        direction={slideDirection}
        in={true}
        mountOnEnter
        unmountOnExit
        timeout={500}
        key={currentDivision.id}
      >
        <div>
          <DataGrid
            rows={paddedRows}
            columns={columns}
            rowSelection={false}
            disableRowSelectionOnClick
            disableColumnMenu={true}
            hideFooter
            autoHeight
            getRowId={row => row.id}
          />
        </div>
      </Slide>

      <PaginationButtons
        count={stages.length}
        currentPage={currentPage}
        onPageChange={handlePageChange}
      />
    </>
  );
};
