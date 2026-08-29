import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { Box, Card, CardContent, Stack, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import {
  confirmDelete,
  notifySuccess,
  notifyWarning,
} from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useTeam } from '@/modules/team/hook/team.hook';
import {
  IAddTeamRequest,
  ITeamResponse,
  IPutTeamRequest,
} from '@/modules/team/type/team.d';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import TeamLogo from '@/views/core/components/TeamLogo';
import JerseySvg from '@/views/core/components/JerseySvg';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import {
  DeleteIcon,
  EditIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';
import TeamsFilterBar from '@/views/team/TeamsFilterBar';
import TeamsTable from '@/views/team/TeamsTable';
import TeamFormDialog from '@/views/team/TeamFormDialog';
import type {
  TeamsSearchFilters,
  TeamFormState,
  TeamFormField,
} from '@/views/team/teams.types';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTERS_DEBOUNCE_DELAY_LONG_MS } from '@/modules/core/constants/constants';

interface TeamsScreenProps {
  tournamentId?: GUID;
  emptyMessage?: string;
  title?: string;
  wrapInCard?: boolean;
  createType?: string;
  onCreate?: () => void;
}

const EMPTY_FILTERS: TeamsSearchFilters = {};

const INITIAL_TEAM_FORM: TeamFormState = {
  name: '',
  threeLetterCode: '',
  shirtColor: '#1E5FCC',
  shirtSecondaryColor: '',
  jerseyStyle: 'solid',
  logo: null,
  logoUrl: '',
};

const TeamsPage: React.FC<TeamsScreenProps> = ({
  tournamentId,
  emptyMessage = 'No hay equipos cargados.',
  title,
  wrapInCard = false,
  createType = 'Equipo',
  onCreate,
}) => {
  const navigate = useNavigate();
  const {
    teams,
    addTeam,
    putTeamById,
    putTeamLogoById,
    getTeamsByFiltered,
    deleteTeamById,
  } = useTeam();
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [filters, setFilters] = useState<TeamsSearchFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<TeamsSearchFilters>(EMPTY_FILTERS);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const getTeamsByFilteredRef = useRef(getTeamsByFiltered);

  useEffect(() => {
    getTeamsByFilteredRef.current = getTeamsByFiltered;
  }, [getTeamsByFiltered]);
  const [teamForm, setTeamForm] = useState<TeamFormState>(INITIAL_TEAM_FORM);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingTeam, setEditingTeam] = useState<ITeamResponse | null>(null);

  const fetchTeams = useCallback(
    async (
      activeFilters: TeamsSearchFilters,
      activePaginationModel: GridPaginationModel
    ) => {
      setLoading(true);
      const response = await getTeamsByFilteredRef.current(
        tournamentId
          ? {
              tournamentId,
              ...activeFilters,
              pageNumber: activePaginationModel.page + 1,
              pageSize: activePaginationModel.pageSize,
            }
          : {
              ...activeFilters,
              pageNumber: activePaginationModel.page + 1,
              pageSize: activePaginationModel.pageSize,
            }
      );
      setRowCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    [tournamentId]
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, FILTERS_DEBOUNCE_DELAY_LONG_MS);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  useEffect(() => {
    void fetchTeams(debouncedFilters, paginationModel);
  }, [debouncedFilters, fetchTeams, paginationModel]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    const updated = {
      ...filters,
      [name]: value || undefined,
    } as TeamsSearchFilters;

    setFilters(updated);
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  };

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

  const resetTeamForm = useCallback(() => {
    setTeamForm(INITIAL_TEAM_FORM);
  }, []);

  const handleTeamFieldChange = useCallback(
    (field: TeamFormField, value: string) => {
      setTeamForm(prev => ({
        ...prev,
        [field]: field === 'threeLetterCode' ? value.toUpperCase() : value,
      }));
    },
    []
  );

  const handleLogoChange = useCallback((file: File | null) => {
    setTeamForm(prev => ({ ...prev, logo: file }));
  }, []);

  const handleView = useCallback(
    (row: ITeamResponse) => {
      navigate(APP_ROUTES.panelTeamDetail.build(row.slug));
    },
    [navigate]
  );

  const handleEdit = useCallback((row: ITeamResponse) => {
    setEditingTeam(row);
    setTeamForm({
      name: row.name,
      threeLetterCode: row.threeLetterCode,
      shirtColor: row.shirtColor,
      shirtSecondaryColor: row.shirtSecondaryColor ?? '',
      jerseyStyle: row.jerseyStyle ?? 'solid',
      logo: null,
      logoUrl: row.logoUrl ?? '',
    });
  }, []);

  const handleDelete = useCallback(
    async (row: ITeamResponse) => {
      const confirmed = await confirmDelete({
        title: '¿Está usted seguro de querer eliminar este equipo?',
        text: '¡Usted no podrá revertir este cambio!',
      });

      if (!confirmed) {
        return;
      }

      await deleteTeamById(row.id);
      await notifySuccess({
        title: '¡Eliminado!',
        text: 'El equipo ha sido eliminado.',
      });
    },
    [deleteTeamById]
  );

  const teamActions = useMemo<TableRowAction<ITeamResponse>[]>(
    () => [
      {
        label: 'Ver',
        color: 'info',
        icon: <VisibilityIcon fontSize="small" />,
        onClick: handleView,
      },
      {
        label: 'Editar',
        color: 'primary',
        icon: <EditIcon fontSize="small" />,
        onClick: handleEdit,
      },
      {
        label: 'Eliminar',
        color: 'error',
        icon: <DeleteIcon fontSize="small" />,
        onClick: handleDelete,
      },
    ],
    [handleDelete, handleEdit, handleView]
  );

  const columns: GridColDef<ITeamResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<ITeamResponse>[] = [
      {
        field: 'name',
        headerName: 'Nombre',
        flex: 1.3,
        minWidth: 220,
        renderCell: params => (
          <Stack direction="row" spacing={1} sx={{
            alignItems: "center"
          }}>
            <TeamLogo
              teamName={params.row.name}
              logoUrl={params.row.logoUrl}
              size={28}
            />
            <Typography variant="body2">{params.row.name}</Typography>
          </Stack>
        ),
      },
      {
        field: 'threeLetterCode',
        headerName: 'Código',
        flex: 0.6,
        minWidth: 110,
      },
      {
        field: 'shirtColor',
        headerName: 'Camiseta',
        flex: 1,
        minWidth: 140,
        sortable: false,
        renderCell: params => (
          <Stack sx={{ height: '100%', justifyContent: 'center' }}>
            <JerseySvg
              color={params.row.shirtColor}
              secondaryColor={params.row.shirtSecondaryColor}
              style={params.row.jerseyStyle}
              size={28}
              title={`Camiseta de ${params.row.name}`}
            />
          </Stack>
        ),
      },
    ];

    return [...baseColumns, buildActionsColumn(teamActions)];
  }, [teamActions]);

  const rows = useMemo(() => teams ?? [], [teams]);
  const hasActiveFilters = useMemo(
    () =>
      Boolean(filters.name) ||
      Boolean(filters.threeLetterCode) ||
      Boolean(filters.shirtColor),
    [filters.name, filters.shirtColor, filters.threeLetterCode]
  );
  const noRowsMessage = hasActiveFilters
    ? 'No se encontraron equipos para el filtro aplicado.'
    : emptyMessage;

  const handleCreateTeam = useCallback(() => {
    if (onCreate) {
      onCreate();
      return;
    }

    resetTeamForm();
    setIsCreateModalOpen(true);
  }, [onCreate, resetTeamForm]);

  const validateCreateForm = () => {
    if (!teamForm.name.trim() || !teamForm.threeLetterCode.trim()) {
      void notifyWarning({
        title: 'Campos incompletos',
        text: 'Nombre y código son obligatorios.',
      });
      return false;
    }

    if (!teamForm.logo) {
      void notifyWarning({
        title: 'Logo requerido',
        text: 'Debe seleccionar un logo para crear el equipo.',
      });
      return false;
    }

    return true;
  };

  const handleCreateSubmit = async () => {
    if (!validateCreateForm()) {
      return;
    }

    setSubmitting(true);
    const payload: IAddTeamRequest = {
      name: teamForm.name.trim(),
      threeLetterCode: teamForm.threeLetterCode.trim(),
      shirtColor: teamForm.shirtColor.trim(),
      shirtSecondaryColor: teamForm.shirtSecondaryColor.trim() || null,
      jerseyStyle: teamForm.jerseyStyle,
      logo: teamForm.logo as File,
      tournamentId,
    };

    const createdTeam = await addTeam(payload);
    setSubmitting(false);

    if (!createdTeam) {
      return;
    }

    setIsCreateModalOpen(false);
    resetTeamForm();
    await fetchTeams(debouncedFilters, paginationModel);
    await notifySuccess({
      title: 'Equipo creado',
      text: 'El equipo se creó correctamente.',
    });
  };

  const handleEditSubmit = async () => {
    if (!editingTeam) {
      return;
    }

    if (!teamForm.name.trim() || !teamForm.threeLetterCode.trim()) {
      void notifyWarning({
        title: 'Campos incompletos',
        text: 'Nombre y código son obligatorios.',
      });
      return;
    }

    setSubmitting(true);
    const payload: IPutTeamRequest = {
      name: teamForm.name.trim(),
      threeLetterCode: teamForm.threeLetterCode.trim(),
      shirtColor: teamForm.shirtColor.trim(),
      shirtSecondaryColor: teamForm.shirtSecondaryColor.trim() || null,
      jerseyStyle: teamForm.jerseyStyle,
    };

    const ok = await putTeamById(editingTeam.id, payload);

    if (!ok) {
      setSubmitting(false);
      return;
    }

    // The team fields and its logo are two separate endpoints; upload the new
    // logo (if the admin picked one) as part of the same save.
    if (teamForm.logo) {
      await putTeamLogoById(editingTeam.id, teamForm.logo);
    }
    setSubmitting(false);

    setEditingTeam(null);
    resetTeamForm();
    await fetchTeams(debouncedFilters, paginationModel);
    await notifySuccess({
      title: 'Equipo actualizado',
      text: 'El equipo se actualizó correctamente.',
    });
  };

  const content = (
    <>
      {(title || createType) && (
        <Stack
          direction="row"
          sx={{
            justifyContent: "space-between",
            alignItems: "center",
            mb: 2
          }}>
          {title ? <Typography variant="h6">{title}</Typography> : <Box />}
          <NewEntityButton type={createType} onClick={handleCreateTeam} />
        </Stack>
      )}

      <TeamsFilterBar filters={filters} onFilterChange={handleFilterChange} />

      <TeamsTable
        rows={rows}
        columns={columns}
        loading={loading}
        noRowsMessage={noRowsMessage}
        paginationModel={paginationModel}
        onPaginationModelChange={handlePaginationModelChange}
        pageSizeOptions={[...TABLE_PAGE_SIZE_OPTIONS]}
        rowCount={rowCount}
      />

      <TeamFormDialog
        withLogo
        open={isCreateModalOpen}
        title="Nuevo equipo"
        confirmLabel="Crear"
        form={teamForm}
        submitting={submitting}
        onFieldChange={handleTeamFieldChange}
        onLogoChange={handleLogoChange}
        onClose={() => {
          setIsCreateModalOpen(false);
          resetTeamForm();
        }}
        onConfirm={() => void handleCreateSubmit()}
      />

      <TeamFormDialog
        withLogo
        open={Boolean(editingTeam)}
        title="Editar equipo"
        confirmLabel="Guardar"
        form={teamForm}
        submitting={submitting}
        onFieldChange={handleTeamFieldChange}
        onLogoChange={handleLogoChange}
        onClose={() => {
          setEditingTeam(null);
          resetTeamForm();
        }}
        onConfirm={() => void handleEditSubmit()}
      />
    </>
  );

  if (wrapInCard) {
    return (
      <Card>
        <CardContent>{content}</CardContent>
      </Card>
    );
  }

  return <Box sx={{ width: '100%' }}>{content}</Box>;
};

export default TeamsPage;
