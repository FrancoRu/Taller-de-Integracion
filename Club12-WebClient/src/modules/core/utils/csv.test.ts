import { describe, expect, it } from 'vitest';
import { buildCsv, parseCsv } from '@/modules/core/utils/csv';

describe('buildCsv', () => {
  it('emits the header row followed by data rows, CRLF-separated', () => {
    const csv = buildCsv(
      ['#', 'Jugador', 'Puntos'],
      [
        [1, 'Ana Gómez', 12],
        [2, 'Beto Ruiz', 9],
      ]
    );

    expect(csv).toBe(
      '#,Jugador,Puntos\r\n1,Ana Gómez,12\r\n2,Beto Ruiz,9'
    );
  });

  it('quotes cells containing commas, quotes or line breaks and doubles inner quotes', () => {
    const csv = buildCsv(
      ['Equipo', 'Nota'],
      [
        ['Club, 12', 'dijo "hola"'],
        ['Salto\nAlto', 'ok'],
      ]
    );

    const lines = csv.split('\r\n');
    expect(lines[0]).toBe('Equipo,Nota');
    expect(lines[1]).toBe('"Club, 12","dijo ""hola"""');
    // A cell with a newline is wrapped in quotes, so the record spans two lines.
    expect(csv).toContain('"Salto\nAlto",ok');
  });

  it('renders null and undefined cells as empty strings', () => {
    const csv = buildCsv(['A', 'B', 'C'], [[null, undefined, 0]]);

    expect(csv).toBe('A,B,C\r\n,,0');
  });
});

describe('parseCsv', () => {
  it('splits the header from data rows and trims header whitespace', () => {
    const parsed = parseCsv('Nombre,Apellido\r\nAna,Gómez\r\nBeto,Ruiz');

    expect(parsed.headers).toEqual(['Nombre', 'Apellido']);
    expect(parsed.rows).toEqual([
      ['Ana', 'Gómez'],
      ['Beto', 'Ruiz'],
    ]);
  });

  it('unescapes quoted cells containing commas and doubled quotes', () => {
    const parsed = parseCsv(
      'Equipo,Nota\r\n"Club, 12","dijo ""hola"""'
    );

    expect(parsed.rows).toEqual([['Club, 12', 'dijo "hola"']]);
  });

  it('strips a leading UTF-8 BOM and skips blank lines', () => {
    const parsed = parseCsv('﻿A,B\r\n1,2\r\n\r\n3,4\r\n');

    expect(parsed.headers).toEqual(['A', 'B']);
    expect(parsed.rows).toEqual([
      ['1', '2'],
      ['3', '4'],
    ]);
  });

  it('returns empty headers/rows for empty input', () => {
    expect(parseCsv('')).toEqual({ headers: [], rows: [] });
  });

  it('round-trips what buildCsv produces', () => {
    const csv = buildCsv(
      ['Nombre', 'Nota'],
      [['Ana, con coma', 'dijo "hola"']]
    );

    const parsed = parseCsv(csv);

    expect(parsed.headers).toEqual(['Nombre', 'Nota']);
    expect(parsed.rows).toEqual([['Ana, con coma', 'dijo "hola"']]);
  });
});
