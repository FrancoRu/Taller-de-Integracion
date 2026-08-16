import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { MenuItem, Stack, TextField } from '@mui/material';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';
import { GUID } from '@/modules/core/types/types';
import { useScorer } from '@/modules/scorer/hook/scorer.hook';
import { IScorerByTeamResponse } from '@/modules/scorer/type/scorer.d';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import DivisionStagePicker from '@/views/core/components/DivisionStagePicker';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

const TeamScorersTab: React.FC = () => {
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { scorersByTeam, getScorersByTeamFiltered } = useScorer();
  const [selectedTournamentId, setSelectedTournamentId] = useState<GUID | ''>(
    ''
  );
  const [selectedDivisionId, setSelectedDivisionId] = useState<GUID | ''>('');
  const [selectedStageId, setSelectedStageId] = useState<GUID | ''>('');
  const [loading, setLoading] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const getAllTournamentsByFilterRef = useRef(getAllTournamentsByFilter);
  const getScorersByTeamFilteredRef = useRef(getScorersByTeamFiltered);

  useEffect(() => {
    getAllTournamentsByFilterRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  useEffect(() => {
    getScorersByTeamFilteredRef.current = getScorersByTeamFiltered;
  }, [getScorersByTeamFiltered]);

  useEffect(() => {
    void getAllTournamentsByFilterRef.current({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, []);

  const fetchScorers = useCallback(
    async (
      activePaginationModel: GridPaginationModel,
      activeTournamentId: GUID | '',
      activeDivisionId: GUID | '',
      activeStageId: GUID | ''
    ) => {
      setLoading(true);

      const response = await getScorersByTeamFilteredRef.current({
        tournamentId: activeTournamentId || undefined,
        divisionId: activeDivisionId || undefined,
        stageId: activeStageId || undefined,
        pageNumber: activePaginationModel.page + 1,
        pageSize: activePaginationModel.pageSize,
      });

      setRowCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    []
  );

  useEffect(() => {
    void fetchScorers(
      paginationModel,
      selectedTournamentId,
      selectedDivisionId,
      selectedStageId
    );
  }, [
    fetchScorers,
    paginationModel,
    selectedDivisionId,
    selectedStageId,
    selectedTournamentId,
  ]);

  const resetToFirstPage = useCallback(() => {
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  }, []);

  const handleTournamentChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      setSelectedTournamentId(event.target.value as GUID | '');
      setSelectedDivisionId('');
      setSelectedStageId('');
      resetToFirstPage();
    },
    [resetToFirstPage]
  );

  const handleDivisionChange = useCallback(
    (divisionId: GUID | '') => {
      setSelectedDivisionId(divisionId);
      resetToFirstPage();
    },
    [resetToFirstPage]
  );

  const handleStageChange = useCallback(
    (stageId: GUID | '') => {
      setSelectedStageId(stageId);
      resetToFirstPage();
    },
    [resetToFirstPage]
  );

  const handlePaginationModelChange = useCallback(
    (nextPaginationModel: GridPaginationModel) => {
      setPaginationModel(prev =>
        prev.page === nextPaginationModel.page &&
        prev.pageSize === nextPaginationModel.pageSize
          ? prev
          : nextPaginationModel
      );
    },
    []
  );

  const columns = useMemo<GridColDef<IScorerByTeamResponse>[]>(
    () => [
      {
        field: 'name',
        headerName: 'Equipo',
        flex: 1.4,
        minWidth: 220,
      },
      {
        field: 'points',
        headerName: 'Puntos',
        flex: 0.8,
        minWidth: 120,
      },
    ],
    []
  );

  return (
    <>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={2} flexWrap="wrap">
        <TextField
          select
          label="Torneo"
          size="small"
          value={selectedTournamentId}
          onChange={handleTournamentChange}
          sx={{ minWidth: 220 }}
        >
          <MenuItem value="">Todos</MenuItem>
          {(tournaments ?? []).map(tournament => (
            <MenuItem key={tournament.id} value={tournament.id}>
              {tournament.name}
            </MenuItem>
          ))}
        </TextField>

        <DivisionStagePicker
          tournamentId={selectedTournamentId}
          divisionId={selectedDivisionId}
          stageId={selectedStageId}
          onDivisionChange={handleDivisionChange}
          onStageChange={handleStageChange}
        />
      </Stack>

      <DataGrid
        rows={scorersByTeam ?? []}
        columns={columns}
        getRowId={row => row.teamId}
        loading={loading}
        autoHeight
        disableRowSelectionOnClick
        disableColumnMenu
        paginationMode="server"
        rowCount={rowCount}
        pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
        paginationModel={paginationModel}
        onPaginationModelChange={handlePaginationModelChange}
        localeText={{
          noRowsLabel: 'No hay puntuaciones registradas por equipo.',
        }}
      />
    </>
  );
};

export default TeamScorersTab;
