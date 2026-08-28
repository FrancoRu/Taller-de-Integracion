import { describe, expect, it } from 'vitest';
import { buildCsv } from '@/modules/core/utils/csv';

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
