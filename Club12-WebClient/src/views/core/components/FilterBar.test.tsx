import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import FilterBar from '@/views/core/components/FilterBar';

describe('FilterBar', () => {
  it('renders its children inside a labelled filter region', () => {
    render(
      <FilterBar>
        <input aria-label="Nombre" />
      </FilterBar>
    );

    expect(
      screen.getByRole('region', { name: 'Filtros' })
    ).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Nombre' })).toBeInTheDocument();
  });

  it('uses a custom aria-label when provided', () => {
    render(
      <FilterBar ariaLabel="Filtros de equipos">
        <input aria-label="Nombre" />
      </FilterBar>
    );

    expect(
      screen.getByRole('region', { name: 'Filtros de equipos' })
    ).toBeInTheDocument();
  });

  it('renders the clear button and fires onClear when clicked', () => {
    const onClear = vi.fn();
    render(
      <FilterBar onClear={onClear}>
        <input aria-label="Nombre" />
      </FilterBar>
    );

    fireEvent.click(screen.getByRole('button', { name: 'Limpiar filtros' }));

    expect(onClear).toHaveBeenCalledTimes(1);
  });

  it('omits the clear button when onClear is not set', () => {
    render(
      <FilterBar>
        <input aria-label="Nombre" />
      </FilterBar>
    );

    expect(
      screen.queryByRole('button', { name: 'Limpiar filtros' })
    ).not.toBeInTheDocument();
  });
});
