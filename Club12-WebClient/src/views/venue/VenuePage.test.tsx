import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import VenuePage from '@/views/venue/venuePage';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import type { IVenueResponse } from '@/modules/venue/type/venue';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/venue/hook/venue.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

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

  it('shows an interactive map when coordinates exist', async () => {
    const getVenueById = vi.fn().mockResolvedValue(
      buildVenue({ latitude: -34.603722, longitude: -58.381592 })
    );
    mockedUseVenue.mockReturnValue({
      getVenueById,
    } as unknown as ReturnType<typeof useVenue>);

    renderAt('/panel/canchas/cancha-central');

    expect(
      await screen.findByRole('group', { name: 'Mapa de Cancha Central' })
    ).toBeInTheDocument();
  });

  it('omits the map when the venue has no coordinates', async () => {
    const getVenueById = vi.fn().mockResolvedValue(buildVenue());
    mockedUseVenue.mockReturnValue({
      getVenueById,
    } as unknown as ReturnType<typeof useVenue>);

    renderAt('/panel/canchas/cancha-central');

    await screen.findByText('Cancha Central');
    expect(
      screen.queryByRole('group', { name: 'Mapa de Cancha Central' })
    ).not.toBeInTheDocument();
  });
});

describe('VenuePage — edit trigger', () => {
  it('opens the edit dialog prefilled with the venue\'s current values', async () => {
    const getVenueById = vi.fn().mockResolvedValue(buildVenue());
    mockedUseVenue.mockReturnValue({
      getVenueById,
      putVenueById: vi.fn(),
      putVenuePhotoById: vi.fn(),
    } as unknown as ReturnType<typeof useVenue>);

    const user = userEvent.setup();
    renderAt('/panel/canchas/cancha-central');

    await user.click(await screen.findByRole('button', { name: 'Editar cancha' }));

    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByRole('textbox', { name: /^Nombre/ })).toHaveValue(
      'Cancha Central'
    );
    expect(
      within(dialog).getByRole('textbox', { name: /^Dirección/ })
    ).toHaveValue('Calle 123');
  });

  it('saves via putVenueById, closes the dialog and shows the updated venue', async () => {
    const getVenueById = vi.fn().mockResolvedValue(buildVenue());
    const putVenueById = vi.fn().mockResolvedValue(buildVenue({ name: 'Cancha Norte' }));
    const putVenuePhotoById = vi.fn().mockResolvedValue(undefined);
    mockedUseVenue.mockReturnValue({
      getVenueById,
      putVenueById,
      putVenuePhotoById,
    } as unknown as ReturnType<typeof useVenue>);

    const user = userEvent.setup();
    renderAt('/panel/canchas/cancha-central');

    await user.click(await screen.findByRole('button', { name: 'Editar cancha' }));

    const dialog = screen.getByRole('dialog');
    const nameInput = within(dialog).getByRole('textbox', { name: /^Nombre/ });
    await user.clear(nameInput);
    await user.type(nameInput, 'Cancha Norte');

    await user.click(within(dialog).getByRole('button', { name: /guardar/i }));

    await waitFor(() => expect(putVenueById).toHaveBeenCalledTimes(1));
    const [id, payload] = putVenueById.mock.calls[0];
    expect(id).toBe('44444444-4444-4444-4444-444444444444');
    expect(payload).toEqual(expect.objectContaining({ name: 'Cancha Norte' }));

    await waitFor(() =>
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    );
    // The refreshed venue comes straight from putVenueById's return value —
    // no second getVenueById round-trip — and the page reflects it right away.
    expect(await screen.findByRole('heading', { name: 'Cancha Norte' })).toBeInTheDocument();
    expect(getVenueById).toHaveBeenCalledTimes(1);
    expect(putVenuePhotoById).not.toHaveBeenCalled();
  });

  it('shows the new photo after a photo upload without a page reload', async () => {
    const getVenueById = vi.fn().mockResolvedValue(buildVenue());
    const putVenueById = vi.fn().mockResolvedValue(buildVenue());
    const putVenuePhotoById = vi
      .fn()
      .mockResolvedValue(buildVenue({ photoUrl: 'https://cdn.test/venue/new.jpg' }));
    mockedUseVenue.mockReturnValue({
      getVenueById,
      putVenueById,
      putVenuePhotoById,
    } as unknown as ReturnType<typeof useVenue>);

    const user = userEvent.setup();
    renderAt('/panel/canchas/cancha-central');

    await user.click(await screen.findByRole('button', { name: 'Editar cancha' }));

    const dialog = screen.getByRole('dialog');
    const fileInput = dialog.querySelector('input[type="file"]') as HTMLInputElement;
    await user.upload(fileInput, new File(['x'], 'court.jpg', { type: 'image/jpeg' }));

    await user.click(within(dialog).getByRole('button', { name: /guardar/i }));

    await waitFor(() => expect(putVenuePhotoById).toHaveBeenCalledTimes(1));
    await waitFor(() =>
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    );
    const image = await screen.findByRole('img', { name: /Cancha Central/ });
    expect(image).toHaveAttribute('src', 'https://cdn.test/venue/new.jpg');
  });

  it('keeps the edit dialog open and does not show a false success when the photo upload fails', async () => {
    // putVenuePhotoById (venue.context.tsx) resolves to undefined on a
    // rejected upload (its own catch calls handleUnknownError and returns
    // nothing) — the page must treat that as a real failure, not close the
    // dialog and report success as if the whole save had gone through.
    const getVenueById = vi.fn().mockResolvedValue(buildVenue());
    const putVenueById = vi.fn().mockResolvedValue(buildVenue());
    const putVenuePhotoById = vi.fn().mockResolvedValue(undefined);
    mockedUseVenue.mockReturnValue({
      getVenueById,
      putVenueById,
      putVenuePhotoById,
    } as unknown as ReturnType<typeof useVenue>);

    const user = userEvent.setup();
    renderAt('/panel/canchas/cancha-central');

    await user.click(await screen.findByRole('button', { name: 'Editar cancha' }));
    const dialog = screen.getByRole('dialog');
    const fileInput = dialog.querySelector('input[type="file"]') as HTMLInputElement;
    await user.upload(fileInput, new File(['x'], 'court.jpg', { type: 'image/jpeg' }));

    const saveButton = within(dialog).getByRole('button', { name: /guardar/i });
    await user.click(saveButton);

    // Wait for the button to re-enable (setEditSubmitting(false), the last
    // thing the early-return failure path does) rather than just "the mock
    // was called" — that fires a tick before the rest of the same async
    // function has actually run its early-return check.
    await waitFor(() => expect(saveButton).toBeEnabled());
    expect(putVenuePhotoById).toHaveBeenCalledTimes(1);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });
});
