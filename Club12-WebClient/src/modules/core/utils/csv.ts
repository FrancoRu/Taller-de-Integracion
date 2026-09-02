/**
 * Minimal, dependency-free CSV export helper (HU-89). Turns a header row plus a
 * matrix of cells into an RFC-4180-style CSV string and triggers a client-side
 * download, so standings, goleadores and fixtures can be shared outside the app
 * without any server round-trip.
 */

export type CsvCellValue = string | number | boolean | null | undefined;
export type CsvRow = CsvCellValue[];

const CSV_DELIMITER = ',';
/** RFC-4180 records are separated by CRLF; Excel and Sheets both expect it. */
const CSV_LINE_BREAK = '\r\n';
/**
 * UTF-8 BOM prepended to the download so spreadsheet apps (notably Excel on
 * Windows) detect UTF-8 and render accented characters (á, ó, ñ) correctly.
 */
const UTF8_BOM = '﻿';

/**
 * Escapes a single CSV cell: renders null/undefined as empty, and wraps the
 * value in double quotes (doubling any inner quote) whenever it contains a
 * delimiter, a quote or a line break.
 */
const escapeCsvCell = (value: CsvCellValue): string => {
  if (value === null || value === undefined) {
    return '';
  }

  const text = String(value);
  if (/[",\r\n]/.test(text)) {
    return `"${text.replace(/"/g, '""')}"`;
  }

  return text;
};

/**
 * Builds a CSV string from a header row and data rows. Pure and
 * side-effect-free so it can be unit-tested in isolation.
 */
export const buildCsv = (headers: string[], rows: CsvRow[]): string =>
  [headers, ...rows]
    .map(row => row.map(escapeCsvCell).join(CSV_DELIMITER))
    .join(CSV_LINE_BREAK);

/**
 * Builds a CSV from the given headers/rows and triggers a browser download of
 * it as `<filename>.csv`.
 */
export const downloadCsv = (
  filename: string,
  headers: string[],
  rows: CsvRow[]
): void => {
  const csv = `${UTF8_BOM}${buildCsv(headers, rows)}`;
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename.toLowerCase().endsWith('.csv')
    ? filename
    : `${filename}.csv`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
};

export interface ParsedCsv {
  headers: string[];
  rows: string[][];
}

/** Splits one CSV record into cells, undoing {@link escapeCsvCell}'s
 * double-quote escaping (a `""` inside a quoted cell is a literal `"`). */
const parseCsvLine = (line: string): string[] => {
  const cells: string[] = [];
  let current = '';
  let inQuotes = false;

  for (let i = 0; i < line.length; i++) {
    const char = line[i];

    if (inQuotes) {
      if (char === '"') {
        if (line[i + 1] === '"') {
          current += '"';
          i++;
        } else {
          inQuotes = false;
        }
      } else {
        current += char;
      }
      continue;
    }

    if (char === '"') {
      inQuotes = true;
    } else if (char === CSV_DELIMITER) {
      cells.push(current);
      current = '';
    } else {
      current += char;
    }
  }

  cells.push(current);
  return cells;
};

/**
 * Parses CSV text — the counterpart to {@link buildCsv} — into a header row
 * and data rows of raw string cells. Blank lines are skipped so a trailing
 * newline (or one left over from editing in a spreadsheet app) doesn't turn
 * into a spurious empty row.
 */
export const parseCsv = (text: string): ParsedCsv => {
  const lines = text
    .replace(/^\uFEFF/, '')
    .split(/\r\n|\n/)
    .filter(line => line.trim() !== '');

  if (lines.length === 0) {
    return { headers: [], rows: [] };
  }

  const [headerLine, ...dataLines] = lines;
  return {
    headers: parseCsvLine(headerLine).map(cell => cell.trim()),
    rows: dataLines.map(parseCsvLine),
  };
};
