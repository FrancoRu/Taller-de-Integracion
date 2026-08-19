import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import TeamsFilterBar from '@/views/team/TeamsFilterBar';
import type { TeamsSearchFilters } from '@/views/team/teams.types';

describe('TeamsFilterBar', () => {
  it('renders the current filter values in the Nombre, Código and Color camiseta fields', () => {
    const filters: TeamsSearchFilters = {
      name: 'River',
      threeLetterCode: 'RIV',
      shirtColor: 'Rojo',
    };

    render(
      <TeamsFilterBar filters={filters} onFilterChange={vi.fn()} />
    );

    expect(screen.getByRole('textbox', { name: 'Nombre' })).toHaveValue(
      'River'
    );
    expect(screen.getByRole('textbox', { name: 'Código' })).toHaveValue(
      'RIV'
    );
    expect(
      screen.getByRole('textbox', { name: 'Color camiseta' })
    ).toHaveValue('Rojo');
  });

  it('fires onFilterChange with the field name and typed value when the user edits a field', () => {
    let capturedName = '';
    let capturedValue = '';
    const onFilterChange = vi.fn(
      (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        capturedName = e.target.name;
        capturedValue = e.target.value;
      }
    );
    const filters: TeamsSearchFilters = {};

    render(
      <TeamsFilterBar filters={filters} onFilterChange={onFilterChange} />
    );

    fireEvent.change(screen.getByRole('textbox', { name: 'Nombre' }), {
      target: { value: 'Boca' },
    });

    expect(onFilterChange).toHaveBeenCalledTimes(1);
    expect(capturedName).toBe('name');
    expect(capturedValue).toBe('Boca');
  });
});
