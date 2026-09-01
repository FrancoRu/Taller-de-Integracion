import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import VenueFormFields from './VenueFormFields';
import { geocodeAddress } from '@/modules/core/utils/geocoding';
import type { VenueFormState } from './venues.types';

vi.mock('@/modules/core/utils/geocoding');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

const mockedGeocodeAddress = vi.mocked(geocodeAddress);

const EMPTY_FORM: VenueFormState = {
  name: 'Cancha Norte',
  address: '',
  latitude: '',
  longitude: '',
  photo: null,
  photoUrl: '',
};

afterEach(() => {
  vi.clearAllMocks();
});

describe('VenueFormFields — buscar en el mapa', () => {
  it('fills latitude/longitude from the geocoded address and shows the map preview', async () => {
    mockedGeocodeAddress.mockResolvedValue({ latitude: -34.6037, longitude: -58.3816 });
    const onFieldChange = vi.fn();
    const user = userEvent.setup();

    const { rerender } = render(
      <VenueFormFields
        withPhoto={false}
        form={{ ...EMPTY_FORM, address: 'Av. Corrientes 1000' }}
        onFieldChange={onFieldChange}
        onPhotoChange={vi.fn()}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Buscar en el mapa' }));

    await waitFor(() => expect(mockedGeocodeAddress).toHaveBeenCalledWith('Av. Corrientes 1000'));
    expect(onFieldChange).toHaveBeenCalledWith('latitude', '-34.6037');
    expect(onFieldChange).toHaveBeenCalledWith('longitude', '-58.3816');

    rerender(
      <VenueFormFields
        withPhoto={false}
        form={{
          ...EMPTY_FORM,
          address: 'Av. Corrientes 1000',
          latitude: '-34.6037',
          longitude: '-58.3816',
        }}
        onFieldChange={onFieldChange}
        onPhotoChange={vi.fn()}
      />
    );

    expect(screen.getByTitle('Mapa de Cancha Norte')).toBeInTheDocument();
  });

  it('does not call the network when the address is empty', async () => {
    const onFieldChange = vi.fn();
    const user = userEvent.setup();

    render(
      <VenueFormFields
        withPhoto={false}
        form={EMPTY_FORM}
        onFieldChange={onFieldChange}
        onPhotoChange={vi.fn()}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Buscar en el mapa' }));

    expect(mockedGeocodeAddress).not.toHaveBeenCalled();
    expect(onFieldChange).not.toHaveBeenCalled();
  });

  it('leaves coordinates untouched when the address cannot be found', async () => {
    mockedGeocodeAddress.mockResolvedValue(null);
    const onFieldChange = vi.fn();
    const user = userEvent.setup();

    render(
      <VenueFormFields
        withPhoto={false}
        form={{ ...EMPTY_FORM, address: 'una dirección inexistente' }}
        onFieldChange={onFieldChange}
        onPhotoChange={vi.fn()}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Buscar en el mapa' }));

    await waitFor(() => expect(mockedGeocodeAddress).toHaveBeenCalledTimes(1));
    expect(onFieldChange).not.toHaveBeenCalled();
  });
});
