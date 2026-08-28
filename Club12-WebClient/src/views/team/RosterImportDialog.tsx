import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Dialog,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useClub } from '@/modules/club/hook/club.hook';
import { GUID } from '@/modules/core/types/types';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import FormButtons from '@/views/core/components/FormButtons';

interface RosterImportDialogProps {
  open: boolean;
  onClose: () => void;
  /** The team the roster is imported INTO (the new season's team). */
  targetTeamId: GUID;
  /** The new season the roster is cloned into. Import needs a target season. */
  targetTournamentId: GUID | null;
  /** The stable club this team belongs to, used to list previous seasons. */
  clubId?: GUID | null;
  /** Refresh the roster after a successful import. */
  onImported?: () => void;
}

interface SourceOption {
  /** `${teamId}|${tournamentId}` — a source team + season pair. */
  value: string;
  teamId: GUID;
  tournamentId: GUID;
  label: string;
}

/**
 * Imports a plantel from a previous season into a new season's team (HU-53).
 * The source options are the sibling per-season teams of the same club; the
 * ficha médica is NOT copied — each player starts Pending in the new season.
 */
const RosterImportDialog: React.FC<RosterImportDialogProps> = ({
  open,
  onClose,
  targetTeamId,
  targetTournamentId,
  clubId,
  onImported,
}) => {
  const { getClubHistory, copyRoster } = useClub();
  const [options, setOptions] = useState<SourceOption[]>([]);
  const [selected, setSelected] = useState('');
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!open) {
      setSelected('');
      return;
    }

    if (!clubId) {
      setOptions([]);
      return;
    }

    const loadSources = async () => {
      setLoading(true);
      const history = await getClubHistory(clubId);
      setLoading(false);

      if (!history) {
        setOptions([]);
        return;
      }

      // Flatten to (team, season) pairs, excluding the current team + the
      // target season itself: you cannot import a roster from the very
      // season you are importing into.
      const sources = history.teams.flatMap(team =>
        team.seasons
          .filter(
            season =>
              !(
                team.teamId === targetTeamId &&
                season.tournamentId === targetTournamentId
              )
          )
          .map<SourceOption>(season => ({
            value: `${team.teamId}|${season.tournamentId}`,
            teamId: team.teamId,
            tournamentId: season.tournamentId,
            label: `${team.name} · ${season.tournamentName ?? 'Temporada'}`,
          }))
      );

      setOptions(sources);
    };

    void loadSources();
  }, [open, clubId, getClubHistory, targetTeamId, targetTournamentId]);

  const selectedOption = useMemo(
    () => options.find(option => option.value === selected),
    [options, selected]
  );

  const handleImport = useCallback(async () => {
    if (!targetTournamentId) {
      await notifyWarning({
        title: 'Temporada requerida',
        text: 'El equipo debe estar asignado a una temporada para importar un plantel.',
      });
      return;
    }

    if (!selectedOption) {
      await notifyWarning({
        title: 'Selección requerida',
        text: 'Debes elegir un plantel de una temporada anterior.',
      });
      return;
    }

    setSubmitting(true);
    const result = await copyRoster(targetTeamId, {
      sourceTeamId: selectedOption.teamId,
      sourceTournamentId: selectedOption.tournamentId,
      targetTournamentId,
    });
    setSubmitting(false);

    if (!result) {
      return;
    }

    await notifySuccess({
      title: 'Plantel importado',
      text: `${result.copiedCount} jugador(es) copiado(s), ${result.skippedCount} omitido(s). La ficha médica no se copia: cada jugador arranca con ficha médica pendiente.`,
    });

    setSelected('');
    onClose();
    onImported?.();
  }, [
    copyRoster,
    onClose,
    onImported,
    selectedOption,
    targetTeamId,
    targetTournamentId,
  ]);

  const handleClose = useCallback(() => {
    if (submitting) {
      return;
    }
    onClose();
  }, [onClose, submitting]);

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Importar plantel de una temporada anterior</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 0.5 }}>
          <Alert severity="info">
            La ficha médica NO se copia: cada jugador arranca con la ficha
            médica pendiente en la nueva temporada.
          </Alert>

          {!clubId ? (
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              Este equipo todavía no está vinculado a un club, por lo que no
              hay temporadas anteriores para importar.
            </Typography>
          ) : (
            <TextField
              select
              required
              label="Plantel origen"
              value={selected}
              onChange={e => setSelected(e.target.value)}
              disabled={loading || options.length === 0}
              helperText={
                loading
                  ? 'Cargando temporadas anteriores…'
                  : options.length === 0
                    ? 'No hay planteles de temporadas anteriores disponibles.'
                    : undefined
              }
              fullWidth
            >
              <MenuItem value="" disabled>
                Seleccionar plantel origen
              </MenuItem>
              {options.map(option => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          )}

          <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
            <FormButtons
              onCancel={handleClose}
              onConfirm={() => void handleImport()}
              confirmLabel="Importar"
              disabled={submitting || !clubId}
            />
          </Stack>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

export default RosterImportDialog;
