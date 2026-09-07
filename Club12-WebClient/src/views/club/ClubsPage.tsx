import { useCallback, useEffect, useMemo, useState } from 'react';
import { GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { DataGrid } from '@mui/x-data-grid';
import { InputAdornment, Stack, Typography } from '@mui/material';
import TextField from '@mui/material/TextField';
import { useNavigate } from 'react-router-dom';
import PageShell from '@/views/core/components/PageShell';
import TableScrollBox from '@/views/core/components/TableScrollBox';
import TeamLogo from '@/views/core/components/TeamLogo';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import { SearchIcon } from '@/views/core/MUI/icons/icons';
import { dataGridLocaleText } from '@/modules/core/constants/dataGridLocale';
import { TABLE_PAGE_SIZE_OPTIONS, TABLE_ROWS_PER_PAGE } from '@/modules/core/constants/pagination';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { useClub } from '@/modules/club/hook/club.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { IClubSummaryResponse } from '@/modules/club/type/club.d';
import { IAddTeamRequest } from '@/modules/team/type/team.d';
import TeamFormDialog from '@/views/team/TeamFormDialog';
import type { TeamFormField, TeamFormState } from '@/views/team/teams.types';

const INITIAL_TEAM_FORM: TeamFormState = {
  name: '',
  threeLetterCode: '',
  shirtColor: '#1E5FCC',
  shirtSecondaryColor: '',
  shirtTertiaryColor: '',
  jerseyStyle: 'solid',
  logo: null,
  logoUrl: '',
};

/** The admin's club roster (the "Equipos" tab): one row per stable club, not per per-season team. */
const ClubsPage: React.FC = () => {
  const navigate = useNavigate();
  const { allClubs, getAllClubs } = useClub();
  const { addTeam } = useTeam();
  const [loading, setLoading] = useState(false);
  const [nameFilter, setNameFilter] = useState('');
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const [teamForm, setTeamForm] = useState<TeamFormState>(INITIAL_TEAM_FORM);
  const [submitting, setSubmitting] = useState(false);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  useEffect(() => {
    const fetchClubs = async () => {
      setLoading(true);
      await getAllClubs();
      setLoading(false);
    };

    void fetchClubs();
  }, [getAllClubs]);

  const normalizedFilter = nameFilter.trim().toLowerCase();
  const rows = useMemo(
    () =>
      normalizedFilter
        ? allClubs.filter(club => club.name.toLowerCase().includes(normalizedFilter))
        : allClubs,
    [allClubs, normalizedFilter]
  );

  const handleView = useCallback(
    (row: IClubSummaryResponse) => {
      navigate(APP_ROUTES.panelClub.build(row.slug));
    },
    [navigate]
  );

  const columns: GridColDef<IClubSummaryResponse>[] = useMemo(
    () => [
      {
        field: 'name',
        headerName: 'Nombre',
        flex: 1,
        minWidth: 260,
        renderCell: params => (
          <Stack
            direction="row"
            spacing={1}
            sx={{ alignItems: 'center', height: '100%', cursor: 'pointer' }}
            onClick={() => handleView(params.row)}
          >
            <TeamLogo teamName={params.row.name} logoUrl={params.row.logoUrl} size={28} />
            <Typography variant="body2">{params.row.name}</Typography>
          </Stack>
        ),
      },
    ],
    [handleView]
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
      shirtTertiaryColor: teamForm.shirtTertiaryColor.trim() || null,
      jerseyStyle: teamForm.jerseyStyle,
      logo: teamForm.logo as File,
    };

    const createdTeam = await addTeam(payload);
    setSubmitting(false);

    if (!createdTeam) {
      return;
    }

    setIsCreateModalOpen(false);
    resetTeamForm();
    await getAllClubs();
    await notifySuccess({
      title: 'Equipo creado',
      text: 'El equipo se creó correctamente.',
    });
  };

  return (
    <PageShell
      title="Equipos"
      actions={
        <NewEntityButton
          type="Equipo"
          onClick={() => {
            resetTeamForm();
            setIsCreateModalOpen(true);
          }}
        />
      }
    >
      <TextField
        label="Nombre"
        size="small"
        value={nameFilter}
        onChange={event => {
          setNameFilter(event.target.value);
          setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
        }}
        sx={{ mb: 2 }}
        slotProps={{
          input: {
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon fontSize="small" />
              </InputAdornment>
            ),
          },
        }}
      />

      <TableScrollBox>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          getRowId={row => row.id}
          autoHeight
          disableRowSelectionOnClick
          disableColumnMenu
          localeText={dataGridLocaleText('No hay clubes cargados.')}
          pageSizeOptions={[...TABLE_PAGE_SIZE_OPTIONS]}
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          onRowClick={params => handleView(params.row)}
        />
      </TableScrollBox>

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
    </PageShell>
  );
};

export default ClubsPage;
