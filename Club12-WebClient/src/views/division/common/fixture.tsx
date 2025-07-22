import React, { useMemo, useState } from 'react';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import Slide from '@mui/material/Slide';
import { MatchResponse, TypeMatch } from '@/modules/match/type/match.d';
import dayjs from 'dayjs';
import { IAllStagePropsView } from '@/modules/stage/type/stage.d';

const TeamCell: React.FC<{
  id: GUID;
  name: string;
  logoUrl: string;
  score?: number;
}> = ({ id, name, logoUrl, score }) => {
  if (!name) return null;
  return (
    <div
      id={id}
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

export const Fixture: React.FC<IAllStagePropsView> = ({ stages }) => {
  const [currentPage, setCurrentPage] = useState(0);
  const [slideDirection, setSlideDirection] = useState<'left' | 'right'>(
    'left'
  );

  const currentDivision = useMemo(() => {
    if (!stages || stages.length === 0) return null;
    const index = stages.length - 1 - currentPage;
    return stages[index] ?? null;
  }, [currentPage, stages]);

  const maxPageSize = useMemo(() => {
    if (!stages) return 0;
    return Math.max(...stages.map(d => d.matchesByWeek?.length ?? 0), 0);
  }, [stages]);

  const paddedRows = useMemo(() => {
    if (!currentDivision) return [];
    const matches = currentDivision.matchesByWeek ?? [];
    const emptyCount = maxPageSize - matches.length;

    const filler = Array.from({ length: emptyCount }, (_, i) => ({
      id: `empty-empty-empty-empty-${i}`,
      matchDate: '',
      type: TypeMatch.Regular,
      matchWeek: 0,
      homeTeamId: '',
      homeTeamName: '',
      homeTeamLogoUrl: '',
      visitorTeamId: '',
      visitorTeamName: '',
      visitorTeamLogoUrl: '',
      homeScore: 0,
      visitorScore: 0,
      isFinished: false,
      winningTeamName: '',
      venue: {
        id: '',
        name: '',
        address: '',
        photoUrl: '',
      },
      isEmpty: true,
    }));

    return [...matches, ...filler];
  }, [currentDivision, maxPageSize]);

  const columns: GridColDef<MatchResponse>[] = useMemo(
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
        valueGetter: params => params.row.venue.name || '',
        sortable: false,
      },
      {
        field: 'HomeTeam',
        headerName: 'Equipo 1',
        flex: 1,
        sortable: false,
        renderCell: params => (
          <TeamCell
            id={params.row.homeTeamId}
            name={params.row.homeTeamName}
            logoUrl={params.row.homeTeamLogoUrl}
            score={params.row.homeScore}
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
            id={params.row.visitorTeamId}
            name={params.row.visitorTeamName}
            logoUrl={params.row.visitorTeamLogoUrl}
            score={params.row.visitorScore}
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
