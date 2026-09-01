import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import VenuesPage from '@/views/venue/VenuesPage';
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

const renderVenuesPage = () =>
  render(
    <MemoryRouter>
      <VenuesPage />
    </MemoryRouter>
  );

afterEach(() => {
  vi.clearAllMocks();
});

describe('VenuesPage — list actions', () => {
  beforeEach(() => {
    mockedUseVenue.mockReturnValue({
      venues: [buildVenue()],
      addVenue: vi.fn(),
      putVenueById: vi.fn(),
      putVenuePhotoById: vi.fn(),
      deleteVenueById: vi.fn(),
      getAllVenues: vi.fn(),
    } as unknown as ReturnType<typeof useVenue>);
  });

  it('does not offer an Editar row action — editing lives inside the venue detail page', async () => {
    renderVenuesPage();

    await screen.findByText('Cancha Central');
    expect(screen.queryByTestId('EditIcon')).not.toBeInTheDocument();
    expect(screen.getByTestId('VisibilityIcon')).toBeInTheDocument();
    expect(screen.getByTestId('DeleteIcon')).toBeInTheDocument();
  });
});

describe('VenuesPage — create dialog', () => {
  it('sends the picked photo as imageFile when creating a venue', async () => {
    const addVenue = vi.fn().mockResolvedValue(buildVenue());
    const getAllVenues = vi.fn();
    mockedUseVenue.mockReturnValue({
      venues: [],
      addVenue,
      putVenueById: vi.fn(),
      putVenuePhotoById: vi.fn(),
      deleteVenueById: vi.fn(),
      getAllVenues,
    } as unknown as ReturnType<typeof useVenue>);

    const user = userEvent.setup();
    renderVenuesPage();

    await user.click(screen.getByRole('button', { name: /nueva cancha/i }));

    const dialog = screen.getByRole('dialog');
    await user.type(within(dialog).getByRole('textbox', { name: /^Nombre/ }), 'Cancha Norte');
    await user.type(
      within(dialog).getByRole('textbox', { name: /^Dirección/ }),
      'Av. Siempre Viva 742'
    );

    const file = new File(['fake'], 'cancha.png', { type: 'image/png' });
    const fileInput = dialog.querySelector('input[type="file"]') as HTMLInputElement;
    await user.upload(fileInput, file);

    await user.click(within(dialog).getByRole('button', { name: /crear/i }));

    await waitFor(() => expect(addVenue).toHaveBeenCalledTimes(1));
    expect(addVenue).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Cancha Norte',
        address: 'Av. Siempre Viva 742',
        imageFile: file,
      })
    );
  });
});
