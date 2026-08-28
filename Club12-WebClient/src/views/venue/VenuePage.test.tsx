import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import VenuePage from '@/views/venue/venuePage';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import type { IVenueResponse } from '@/modules/venue/type/venue';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/venue/hook/venue.hook');

const mockedUseVenue = vi.mocked(useVenue);

const buildVenue = (overrides: Partial<IVenueResponse> = {}): IVenueResponse => ({
  id: '44444444-4444-4444-4444-444444444444' as GUID,
  slug: 'cancha-central',
  name: 'Cancha Central',
  address: 'Calle 123',
  ...overrides,
});

const renderAt = (path: string) =>
  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/panel/canchas/:venueId" element={<VenuePage />} />
      </Routes>
    </MemoryRouter>
  );

describe('VenuePage — HU-15 slug route', () => {
  it('resolves the venue detail route by slug (idOrSlug) and renders it', async () => {
    const getVenueById = vi.fn().mockResolvedValue(buildVenue());
    mockedUseVenue.mockReturnValue({
      getVenueById,
    } as unknown as ReturnType<typeof useVenue>);

    renderAt('/panel/canchas/cancha-central');

    expect(await screen.findByText('Cancha Central')).toBeInTheDocument();
    expect(getVenueById).toHaveBeenCalledWith('cancha-central');
  });
});
