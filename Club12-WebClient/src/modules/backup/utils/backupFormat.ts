/**
 * Spanish display labels for each `IBackupRecordResponse.origin` value,
 * shown in the "Forma de creación" table column.
 */
export const BACKUP_ORIGIN_LABELS: Record<'Manual' | 'Job', string> = {
  Manual: 'Manual',
  Job: 'Programado',
};

const UNITS = ['B', 'KB', 'MB', 'GB', 'TB'] as const;

/**
 * Formats a byte count into a human-readable string (e.g. `1536` → `1.5 KB`),
 * shown in the "Peso" table column.
 * @param {number} bytes - The size in bytes.
 * @returns {string} The formatted, human-readable size.
 */
export const formatBytes = (bytes: number): string => {
  if (!Number.isFinite(bytes) || bytes <= 0) {
    return '0 B';
  }

  const exponent = Math.min(
    Math.floor(Math.log(bytes) / Math.log(1024)),
    UNITS.length - 1
  );
  const value = bytes / Math.pow(1024, exponent);
  // Round to 1 decimal, then drop a trailing ".0" so whole numbers (e.g.
  // exactly 2 KB) read as "2 KB" instead of "2.0 KB".
  const formattedValue =
    exponent === 0 ? value.toString() : value.toFixed(1).replace(/\.0$/, '');

  return `${formattedValue} ${UNITS[exponent]}`;
};
