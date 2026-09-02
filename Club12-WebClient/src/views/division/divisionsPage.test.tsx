import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import DivisionsPage from '@/views/division/divisionsPage';
import { useDivision } from '@/modules/division/hook/division.hook';
import type { IDivisionResponse } from '@/modules/division/type/division';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/division/hook/division.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

import Swal from 'sweetalert2';

const mockedUseDivision = vi.mocked(useDivision);
const mockedSwalFire = vi.mocked(Swal.fire);

const buildDivision = (
  overrides: Partial<IDivisionResponse> = {}
): IDivisionResponse =>
  ({
    id: 'division-1' as unknown as GUID,
    name: 'Zona A',
    slug: 'zona-a',
    isFinished: false,
    positions: [],
    ...overrides,
  }) as IDivisionResponse;

const renderPage = () =>
  render(
    <MemoryRouter>
      <DivisionsPage />
    </MemoryRouter>
  );

afterEach(() => {
  vi.clearAllMocks();
});

describe('DivisionsPage — delete failure', () => {
  it('does not show a success dialog or refetch when deleteDivisionsById fails', async () => {
    const getDivisionsByFilters = vi.fn().mockResolvedValue({
      items: [buildDivision()],
      totalCount: 1,
    });
    const deleteDivisionsById = vi.fn().mockResolvedValue(false);
    mockedUseDivision.mockReturnValue({
      getDivisionsByFilters,
      deleteDivisionsById,
    } as unknown as ReturnType<typeof useDivision>);
    mockedSwalFire.mockResolvedValue({
      isConfirmed: true,
      isDenied: false,
      isDismissed: false,
    } as Awaited<ReturnType<typeof Swal.fire>>);

    renderPage();

    await screen.findByText('Zona A');
    getDivisionsByFilters.mockClear();

    const deleteIcon = await screen.findByTestId('DeleteIcon');
    (deleteIcon.closest('button') as HTMLButtonElement).click();

    await waitFor(() => expect(deleteDivisionsById).toHaveBeenCalledTimes(1));

    expect(mockedSwalFire).not.toHaveBeenCalledWith(
      expect.objectContaining({ title: '¡Eliminada!' })
    );
    expect(getDivisionsByFilters).not.toHaveBeenCalled();
  });
});
