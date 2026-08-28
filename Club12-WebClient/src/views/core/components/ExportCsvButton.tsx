import { Button } from '@mui/material';
import FileDownloadIcon from '@mui/icons-material/FileDownload';

interface ExportCsvButtonProps {
  /** Runs the export (build + download). May be async when it fetches first. */
  onExport: () => void | Promise<void>;
  label?: string;
  disabled?: boolean;
}

/**
 * Shared "Exportar CSV" action button (HU-89) used by the standings,
 * goleadores and fixture views so the export affordance looks the same
 * everywhere. The download logic itself lives in each view (and the
 * `csv` helper); this component only renders the button.
 */
export default function ExportCsvButton({
  onExport,
  label = 'Exportar CSV',
  disabled = false,
}: ExportCsvButtonProps) {
  return (
    <Button
      variant="outlined"
      size="small"
      startIcon={<FileDownloadIcon />}
      onClick={() => void onExport()}
      disabled={disabled}
      sx={{ height: 32, minHeight: 32 }}
    >
      {label}
    </Button>
  );
}
