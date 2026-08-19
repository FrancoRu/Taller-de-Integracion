import { fireEvent, render, screen } from '@testing-library/react';
import { GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { describe, expect, it, vi } from 'vitest';
import TeamsTable from '@/views/team/TeamsTable';
import type { ITeamResponse } from '@/modules/team/type/team.d';
import type { GUID } from '@/modules/core/types/types';

const stubLayoutDimensions = () => {
  Object.defineProperties(window.HTMLElement.prototype, {
    offsetWidth: {
      configurable: true,
      get() {
        return 1000;
      },
    },
    offsetHeight: {
      configurable: true,
      get() {
        return 1000;
      },
    },
    clientWidth: {
      configurable: true,
      get() {
        return 1000;
      },
    },
    clientHeight: {
      configurable: true,
      get() {
        return 1000;
      },
    },
  });
  window.HTMLElement.prototype.getBoundingClientRect = () =>
    (({
      width: 1000,
      height: 1000,
      top: 0,
      left: 0,
      right: 1000,
      bottom: 1000,
      x: 0,
      y: 0,
      toJSON() {}
    }) as DOMRect);
};

if (!window.ResizeObserver) {
  window.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  } as unknown as typeof ResizeObserver;
}
stubLayoutDimensions();

const rows: ITeamResponse[] = [
  {
    id: 'guid-1-aaaa-bbbb-cccc' as unknown as GUID,
    name: 'River',
    slug: 'river',
    threeLetterCode: 'RIV',
    shirtColor: 'Rojo',
    logoUrl: '',
    players: [],
    tournamentId: null,
  },
];

const columns: GridColDef<ITeamResponse>[] = [
  { field: 'name', headerName: 'Nombre', flex: 1 },
];

const paginationModel: GridPaginationModel = { page: 0, pageSize: 10 };

describe('TeamsTable', () => {
  it('renders the provided rows and columns', () => {
    render(
      <TeamsTable
        rows={rows}
        columns={columns}
        loading={false}
        noRowsMessage="No hay equipos cargados."
        paginationModel={paginationModel}
        onPaginationModelChange={vi.fn()}
        pageSizeOptions={[10, 25, 50]}
        rowCount={rows.length}
      />
    );

    expect(screen.getByText('River')).toBeInTheDocument();
  });

  it('shows a loading progressbar when loading is true', () => {
    render(
      <TeamsTable
        rows={rows}
        columns={columns}
        loading
        noRowsMessage="No hay equipos cargados."
        paginationModel={paginationModel}
        onPaginationModelChange={vi.fn()}
        pageSizeOptions={[10, 25, 50]}
        rowCount={rows.length}
      />
    );

    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('fires onPaginationModelChange when the page is changed', () => {
    const onPaginationModelChange = vi.fn();
    const manyRows: ITeamResponse[] = Array.from({ length: 15 }, (_, i) => ({
      id: `guid-row-${i}-bbbb-cccc` as unknown as GUID,
      name: `Team ${i}`,
      slug: `team-${i}`,
      threeLetterCode: `T${i}`,
      shirtColor: 'Rojo',
      logoUrl: '',
      players: [],
      tournamentId: null,
    }));

    render(
      <TeamsTable
        rows={manyRows}
        columns={columns}
        loading={false}
        noRowsMessage="No hay equipos cargados."
        paginationModel={paginationModel}
        onPaginationModelChange={onPaginationModelChange}
        pageSizeOptions={[10, 25, 50]}
        rowCount={manyRows.length}
      />
    );

    fireEvent.click(screen.getByRole('button', { name: /go to next page/i }));

    expect(onPaginationModelChange).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1 }),
      expect.anything()
    );
  });
});
