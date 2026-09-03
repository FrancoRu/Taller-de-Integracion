import { useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogContent,
  DialogTitle,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { downloadCsv, parseCsv } from '@/modules/core/utils/csv';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { extractProblemDetail } from '@/modules/core/utils/problemDetails';
import { playerService } from '@/modules/player/service/player.service';
import { IAddPlayerRequest } from '@/modules/player/type/player.d';
import {
  PlayerFieldsForValidation,
  validatePlayerFields,
} from '@/views/player/players.types';
import FormButtons from '@/views/core/components/FormButtons';
import ExportCsvButton from '@/views/core/components/ExportCsvButton';
import { UploadFileIcon } from '@/views/core/MUI/icons/icons';

const CSV_HEADERS = [
  'Nombre',
  'Segundo nombre',
  'Apellido',
  'Documento',
  'Fecha de nacimiento',
  'Teléfono',
  'Obra social',
];

interface CsvRowResult {
  rowNumber: number;
  fields: PlayerFieldsForValidation;
  /** Pre-submit validation failure — permanent, never retried. */
  error: string | null;
  /** Set once this row has actually been sent to the API. */
  imported: boolean;
  /** The backend's exact reason this row failed to create, if it did. Shown
   * inline per row instead of a global alert — firing one alert per failed
   * row in a batch stacked them on top of each other with no way to read
   * which row failed for what reason. */
  submitError: string | null;
}

const cellsToFields = (cells: string[]): PlayerFieldsForValidation => ({
  firstName: (cells[0] ?? '').trim(),
  secondName: (cells[1] ?? '').trim() || undefined,
  lastName: (cells[2] ?? '').trim(),
  documentNumber: (cells[3] ?? '').replace(/\D/g, ''),
  birthDate: (cells[4] ?? '').trim(),
  phoneNumber: (cells[5] ?? '').trim(),
  socialSecurity: (cells[6] ?? '').trim(),
});

interface RosterCsvImportDialogProps {
  open: boolean;
  onClose: () => void;
  /** The team the imported roster is added to. */
  teamId: GUID;
  onImported?: () => void;
}

/**
 * Imports players in bulk from a CSV file into a team's roster (HU: replaces
 * the old "importar plantel de una temporada anterior" flow). Each row is
 * validated with the exact same rules as the roster table's inline add row
 * ({@link validatePlayerFields}) before anything is sent to the API, and
 * only the rows that pass are actually submitted — the rest are shown
 * inline so the admin can fix and re-upload just those. Rows are submitted
 * via `playerService` directly (not the `usePlayer` context) so a per-row
 * API failure never fires the app's global error alert — with N rows in
 * flight that alert would fire N times and stack, hiding which row failed
 * and why. Each row's outcome is shown in its own "Estado" cell instead.
 */
const RosterCsvImportDialog: React.FC<RosterCsvImportDialogProps> = ({
  open,
  onClose,
  teamId,
  onImported,
}) => {
  const [fileName, setFileName] = useState('');
  const [rows, setRows] = useState<CsvRowResult[]>([]);
  const [submitting, setSubmitting] = useState(false);

  const reset = () => {
    setFileName('');
    setRows([]);
  };

  const handleClose = () => {
    if (submitting) {
      return;
    }
    reset();
    onClose();
  };

  const handleDownloadTemplate = () =>
    downloadCsv('plantel-plantilla', CSV_HEADERS, []);

  const handleFileSelected = async (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) {
      return;
    }

    const text = await file.text();
    const { rows: csvRows } = parseCsv(text);

    if (csvRows.length === 0) {
      await notifyWarning({
        title: 'Archivo vacío',
        text: 'El CSV no tiene filas de jugadores para importar.',
      });
      return;
    }

    setFileName(file.name);
    setRows(
      csvRows.map((cells, index) => {
        const fields = cellsToFields(cells);
        const validationError = validatePlayerFields(fields, teamId);
        return {
          rowNumber: index + 1,
          fields,
          error: validationError ? validationError.text : null,
          imported: false,
          submitError: null,
        };
      })
    );
  };

  const invalidRows = rows.filter(row => row.error !== null);
  // Rows still to (re)try: passed validation and haven't been imported yet —
  // excludes rows already imported by a previous click, so retrying after a
  // partial failure doesn't re-submit (and duplicate-document-conflict) the
  // ones that already succeeded.
  const pendingRows = rows.filter(row => row.error === null && !row.imported);
  const importedRows = rows.filter(row => row.imported);

  const handleImport = async () => {
    if (pendingRows.length === 0) {
      return;
    }

    setSubmitting(true);
    const outcomes = new Map<number, { imported: boolean; submitError: string | null }>();

    for (const row of pendingRows) {
      const payload: IAddPlayerRequest = {
        firstName: row.fields.firstName.trim(),
        secondName: row.fields.secondName?.trim() || undefined,
        lastName: row.fields.lastName.trim(),
        documentNumber: row.fields.documentNumber.trim(),
        birthDate: new Date(row.fields.birthDate),
        phoneNumber: row.fields.phoneNumber.trim(),
        socialSecurity: row.fields.socialSecurity.trim(),
        teamId,
      };

      try {
        await playerService.addPlayer(payload);
        outcomes.set(row.rowNumber, { imported: true, submitError: null });
      } catch (error) {
        outcomes.set(row.rowNumber, {
          imported: false,
          submitError: extractProblemDetail(error) ?? 'No se pudo crear el jugador.',
        });
      }
    }

    setRows(prev =>
      prev.map(row => {
        const outcome = outcomes.get(row.rowNumber);
        return outcome ? { ...row, ...outcome } : row;
      })
    );
    setSubmitting(false);

    const importedNowCount = [...outcomes.values()].filter(o => o.imported).length;
    const failedCount = pendingRows.length - importedNowCount;

    if (importedNowCount > 0) {
      onImported?.();
    }

    if (failedCount === 0) {
      await notifySuccess({
        title: 'Importación finalizada',
        text: `${importedNowCount} jugador(es) importado(s) correctamente.`,
      });
      reset();
      onClose();
    }
    // On a partial/total failure the dialog stays open: each row's exact
    // reason is right there in the "Estado" column, and "Importar" now only
    // re-targets the rows still pending.
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Importar plantel desde CSV</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 0.5 }}>
          <Alert severity="info">
            Cada fila crea un jugador nuevo en este equipo. Columnas
            esperadas, en este orden: {CSV_HEADERS.join(', ')}.
          </Alert>

          <Stack direction="row" spacing={1.5} sx={{ flexWrap: 'wrap' }}>
            <ExportCsvButton
              onExport={handleDownloadTemplate}
              label="Descargar plantilla"
            />
            <Button
              component="label"
              variant="contained"
              size="small"
              startIcon={<UploadFileIcon />}
              sx={{ height: 32, minHeight: 32 }}
            >
              Elegir archivo CSV
              <input
                type="file"
                accept=".csv,text/csv"
                hidden
                onChange={e => void handleFileSelected(e)}
              />
            </Button>
          </Stack>

          {fileName && (
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              {fileName} · {rows.length} fila(s), {pendingRows.length + importedRows.length}{' '}
              válida(s)
              {importedRows.length > 0 && `, ${importedRows.length} ya importada(s)`}
              {invalidRows.length > 0 && `, ${invalidRows.length} con errores`}
              .
            </Typography>
          )}

          {rows.length > 0 && (
            <TableContainer sx={{ maxHeight: 320 }}>
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>#</TableCell>
                    <TableCell>Jugador</TableCell>
                    <TableCell>Estado</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {rows.map(row => {
                    const status = row.error
                      ? row.error
                      : row.imported
                        ? 'Importado'
                        : (row.submitError ?? 'Lista para importar');
                    const isOk = !row.error && !row.submitError;

                    return (
                      <TableRow key={row.rowNumber}>
                        <TableCell>{row.rowNumber}</TableCell>
                        <TableCell>
                          {[row.fields.firstName, row.fields.lastName]
                            .filter(Boolean)
                            .join(' ') || '—'}
                        </TableCell>
                        <TableCell sx={{ color: isOk ? 'success.main' : 'error.main' }}>
                          {status}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          <FormButtons
            onCancel={handleClose}
            onConfirm={() => void handleImport()}
            confirmLabel={`Importar (${pendingRows.length})`}
            disabled={submitting}
            confirmDisabled={pendingRows.length === 0}
          />
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

export default RosterCsvImportDialog;
