import { act, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
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

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
  vi.clearAllMocks();
});

describe('VenueFormFields — auto-geocoding the address', () => {
  it('debounces, geocodes the typed address and fills latitude/longitude', async () => {
    mockedGeocodeAddress.mockResolvedValue({ latitude: -34.6037, longitude: -58.3816 });
    const onFieldChange = vi.fn();

    render(
      <VenueFormFields
        withPhoto={false}
        form={{ ...EMPTY_FORM, address: 'Av. Corrientes 1000' }}
        onFieldChange={onFieldChange}
        onPhotoChange={vi.fn()}
      />
    );

    expect(mockedGeocodeAddress).not.toHaveBeenCalled();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1000);
    });

    expect(mockedGeocodeAddress).toHaveBeenCalledWith('Av. Corrientes 1000');
    expect(onFieldChange).toHaveBeenCalledWith('latitude', '-34.6037');
    expect(onFieldChange).toHaveBeenCalledWith('longitude', '-58.3816');
  });

  it('does not call the network when the address is empty', async () => {
    const onFieldChange = vi.fn();

    render(
      <VenueFormFields
        withPhoto={false}
        form={EMPTY_FORM}
        onFieldChange={onFieldChange}
        onPhotoChange={vi.fn()}
      />
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1000);
    });

    expect(mockedGeocodeAddress).not.toHaveBeenCalled();
    expect(onFieldChange).not.toHaveBeenCalled();
  });

  it('leaves coordinates untouched when the address cannot be found', async () => {
    mockedGeocodeAddress.mockResolvedValue(null);
    const onFieldChange = vi.fn();

    render(
      <VenueFormFields
        withPhoto={false}
        form={{ ...EMPTY_FORM, address: 'una dirección inexistente' }}
        onFieldChange={onFieldChange}
        onPhotoChange={vi.fn()}
      />
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1000);
    });

    expect(mockedGeocodeAddress).toHaveBeenCalledTimes(1);
    expect(onFieldChange).not.toHaveBeenCalled();
  });

  it('always renders an interactive map, even with no coordinates yet (defaults to Paraná)', () => {
    render(
      <VenueFormFields
        withPhoto={false}
        form={EMPTY_FORM}
        onFieldChange={vi.fn()}
        onPhotoChange={vi.fn()}
      />
    );

    expect(
      screen.getByRole('group', { name: 'Mapa de Cancha Norte' })
    ).toBeInTheDocument();
  });

  it('renders the map interactively at the given coordinates', () => {
    render(
      <VenueFormFields
        withPhoto={false}
        form={{ ...EMPTY_FORM, latitude: '-31.7333', longitude: '-60.5297' }}
        onFieldChange={vi.fn()}
        onPhotoChange={vi.fn()}
      />
    );

    const map = screen.getByRole('group', { name: 'Mapa de Cancha Norte' });
    expect(map.querySelector('.leaflet-container')).toBeInTheDocument();
  });
});
