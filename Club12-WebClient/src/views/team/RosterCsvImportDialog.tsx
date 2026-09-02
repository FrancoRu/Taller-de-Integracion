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
import { usePlayer } from '@/modules/player/hook/player.hook';
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
  error: string | null;
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
 * inline so the admin can fix and re-upload just those.
 */
const RosterCsvImportDialog: React.FC<RosterCsvImportDialogProps> = ({
  open,
  onClose,
  teamId,
  onImported,
}) => {
  const { addPlayer } = usePlayer();
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
        };
      })
    );
  };

  const validRows = rows.filter(row => row.error === null);
  const invalidRows = rows.filter(row => row.error !== null);

  const handleImport = async () => {
    if (validRows.length === 0) {
      return;
    }

    setSubmitting(true);
    let importedCount = 0;
    for (const row of validRows) {
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

      const created = await addPlayer(payload);
      if (created) {
        importedCount += 1;
      }
    }
    setSubmitting(false);

    const failedCount = validRows.length - importedCount;
    await notifySuccess({
      title: 'Importación finalizada',
      text: [
        `${importedCount} jugador(es) importado(s) correctamente.`,
        failedCount > 0 ? `${failedCount} fila(s) válida(s) fallaron al crearse (ver el detalle mostrado).` : null,
        invalidRows.length > 0 ? `${invalidRows.length} fila(s) se omitieron por datos inválidos.` : null,
      ]
        .filter(Boolean)
        .join(' '),
    });

    reset();
    onClose();
    onImported?.();
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
              {fileName} · {rows.length} fila(s), {validRows.length}{' '}
              válida(s)
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
                  {rows.map(row => (
                    <TableRow key={row.rowNumber}>
                      <TableCell>{row.rowNumber}</TableCell>
                      <TableCell>
                        {[row.fields.firstName, row.fields.lastName]
                          .filter(Boolean)
                          .join(' ') || '—'}
                      </TableCell>
                      <TableCell sx={{ color: row.error ? 'error.main' : 'success.main' }}>
                        {row.error ?? 'Lista para importar'}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          <FormButtons
            onCancel={handleClose}
            onConfirm={() => void handleImport()}
            confirmLabel={`Importar (${validRows.length})`}
            disabled={submitting || validRows.length === 0}
          />
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

export default RosterCsvImportDialog;
