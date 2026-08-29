import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { MenuItem, Stack, TextField } from '@mui/material';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';
import { GUID } from '@/modules/core/types/types';
import { useScorer } from '@/modules/scorer/hook/scorer.hook';
import { scorerService } from '@/modules/scorer/service/scorer.service';
import {
  IScorerByPlayerResponse,
  ScorerScope,
} from '@/modules/scorer/type/scorer.d';
import {
  buildScorerScopeParams,
  ScorerScopeParams,
} from '@/modules/scorer/utils/scorerScope';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import DivisionStagePicker from '@/views/core/components/DivisionStagePicker';
import ScorerScopeToggle from '@/views/scorer/ScorerScopeToggle';
import ExportCsvButton from '@/views/core/components/ExportCsvButton';
import { downloadCsv } from '@/modules/core/utils/csv';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { dataGridLocaleText } from '@/modules/core/constants/dataGridLocale';

/** Upper bound of rows fetched in one page for a CSV export (HU-89). */
const CSV_EXPORT_PAGE_SIZE = 1000;
const GOLEADORES_CSV_HEADERS = ['#', 'Jugador', 'Puntos'];

const PlayerScorersTab: React.FC = () => {
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { scorersByPlayer, getScorersByPlayerFiltered } = useScorer();
  const [scope, setScope] = useState<ScorerScope>('tournament');
  const [selectedSeason, setSelectedSeason] = useState<number | ''>('');
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
  const getScorersByPlayerFilteredRef = useRef(getScorersByPlayerFiltered);

  useEffect(() => {
    getAllTournamentsByFilterRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  useEffect(() => {
    getScorersByPlayerFilteredRef.current = getScorersByPlayerFiltered;
  }, [getScorersByPlayerFiltered]);

  useEffect(() => {
    void getAllTournamentsByFilterRef.current({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, []);

  // Season years the ranking can be scoped to (HU-85) — the calendar year of
  // each tournament's start date, most recent first.
  const seasonOptions = useMemo(() => {
    const years = new Set<number>();
    (tournaments ?? []).forEach(tournament => {
      const year = new Date(tournament.startDate).getFullYear();
      if (!Number.isNaN(year)) {
        years.add(year);
      }
    });
    return Array.from(years).sort((a, b) => b - a);
  }, [tournaments]);

  // The active scope (per tournament / per season / all-time) resolved to the
  // exact Scorer/by-player query params (HU-85).
  const scopeParams = useMemo(
    () =>
      buildScorerScopeParams(scope, {
        tournamentId: selectedTournamentId,
        divisionId: selectedDivisionId,
        stageId: selectedStageId,
        season: selectedSeason,
      }),
    [
      scope,
      selectedDivisionId,
      selectedSeason,
      selectedStageId,
      selectedTournamentId,
    ]
  );

  const fetchScorers = useCallback(
    async (
      activePaginationModel: GridPaginationModel,
      activeScopeParams: ScorerScopeParams
    ) => {
      setLoading(true);

      const response = await getScorersByPlayerFilteredRef.current({
        ...activeScopeParams,
        pageNumber: activePaginationModel.page + 1,
        pageSize: activePaginationModel.pageSize,
      });

      setRowCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    []
  );

  useEffect(() => {
    void fetchScorers(paginationModel, scopeParams);
  }, [fetchScorers, paginationModel, scopeParams]);

  const resetToFirstPage = useCallback(() => {
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  }, []);

  const handleScopeChange = useCallback(
    (nextScope: ScorerScope) => {
      setScope(nextScope);
      resetToFirstPage();
    },
    [resetToFirstPage]
  );

  const handleSeasonChange = useCallback(
    (season: number | '') => {
      setSelectedSeason(season);
      resetToFirstPage();
    },
    [resetToFirstPage]
  );

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

  const [exporting, setExporting] = useState(false);

  const handleExportCsv = useCallback(async () => {
    setExporting(true);
    try {
      const response = await scorerService.getScorersByPlayerFiltered({
        ...scopeParams,
        pageNumber: 1,
        pageSize: CSV_EXPORT_PAGE_SIZE,
      });
      const items = response.data?.items ?? [];
      downloadCsv(
        'goleadores',
        GOLEADORES_CSV_HEADERS,
        items.map((row, index) => [index + 1, row.fullName, row.points])
      );
    } finally {
      setExporting(false);
    }
  }, [scopeParams]);

  const columns = useMemo<GridColDef<IScorerByPlayerResponse>[]>(
    () => [
      {
        field: 'fullName',
        headerName: 'Jugador',
        flex: 1.4,
        minWidth: 220,
      },
      {
        field: 'points',
        headerName: 'Puntos',
        flex: 0.8,
        minWidth: 120,
        align: 'center',
        headerAlign: 'center',
      },
    ],
    []
  );

  return (
    <>
      <Stack spacing={2} sx={{ mb: 2 }}>
        <ScorerScopeToggle
          scope={scope}
          onScopeChange={handleScopeChange}
          season={selectedSeason}
          onSeasonChange={handleSeasonChange}
          seasonOptions={seasonOptions}
        />

        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ flexWrap: 'wrap' }}
        >
          {scope === 'tournament' && (
            <>
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
            </>
          )}

          <ExportCsvButton
            onExport={handleExportCsv}
            disabled={exporting || (scorersByPlayer ?? []).length === 0}
          />
        </Stack>
      </Stack>

      <DataGrid
        rows={scorersByPlayer ?? []}
        columns={columns}
        getRowId={row => row.playerId}
        loading={loading}
        autoHeight
        disableRowSelectionOnClick
        disableColumnMenu
        paginationMode="server"
        rowCount={rowCount}
        pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
        paginationModel={paginationModel}
        onPaginationModelChange={handlePaginationModelChange}
        localeText={dataGridLocaleText(
          'No hay puntuaciones registradas por jugador.'
        )}
      />
    </>
  );
};

export default PlayerScorersTab;
