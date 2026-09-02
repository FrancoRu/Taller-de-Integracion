import { act, renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import Swal from 'sweetalert2';
import { ErrorProvider } from '@/modules/error/context/error.context';
import { VenueProvider } from '@/modules/venue/context/venue.context';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import { venueService } from '@/modules/venue/service/venue.service';
import type { GUID } from '@/modules/core/types/types';
import type { IVenueResponse } from '@/modules/venue/type/venue';

vi.mock('@/modules/venue/service/venue.service');
vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
    getContainer: vi.fn().mockReturnValue(null),
  },
}));

const mockedGetAllVenues = vi.mocked(venueService.getAllVenues);
const mockedGetVenueById = vi.mocked(venueService.getVenueById);
const mockedPutVenueById = vi.mocked(venueService.putVenueById);
const mockedPutVenuePhotoById = vi.mocked(venueService.putVenuePhotoById);
const mockedSwalFire = vi.mocked(Swal.fire);

const VENUE_ID = '44444444-4444-4444-4444-444444444444' as GUID;

const buildVenue = (overrides: Partial<IVenueResponse> = {}): IVenueResponse => ({
  id: VENUE_ID,
  slug: 'cancha-central',
  name: 'Cancha Central',
  address: 'Calle 123',
  photoUrl: 'https://cdn.test/venue/old.jpg',
  ...overrides,
});

const wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={new QueryClient()}>
    <ErrorProvider>
      <VenueProvider>{children}</VenueProvider>
    </ErrorProvider>
  </QueryClientProvider>
);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('VenueProvider — no duplicate success toast', () => {
  /**
   * venuePage.tsx / VenuesPage.tsx already show their own "Cancha actualizada"
   * confirmation for these actions. The context used to ALSO fire a generic
   * toast, so the user saw two modals with the same message for one save.
   */
  it('does not fire its own toast after putVenueById succeeds (200)', async () => {
    mockedPutVenueById.mockResolvedValueOnce({
      status: 200,
      data: buildVenue({ name: 'Cancha Norte' }),
    } as never);

    const { result } = renderHook(() => useVenue(), { wrapper });
    await act(async () => {
      await result.current.putVenueById(VENUE_ID, {
        name: 'Cancha Norte',
        address: 'Calle 123',
      });
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('does not fire its own toast after putVenueById succeeds (204)', async () => {
    mockedPutVenueById.mockResolvedValueOnce({ status: 204 } as never);

    const { result } = renderHook(() => useVenue(), { wrapper });
    await act(async () => {
      await result.current.putVenueById(VENUE_ID, {
        name: 'Cancha Norte',
        address: 'Calle 123',
      });
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });
});

describe('VenueProvider — photo upload refreshes the venue', () => {
  /**
   * The photo endpoint returns no body and every upload lands at a fresh
   * unique URL (SupabaseHelper.GenerateNameFile), so the new photoUrl is only
   * knowable via a real GET. getVenueById short-circuits on the in-memory
   * `venues` list, which still holds the pre-upload URL — so putVenuePhotoById
   * must hit the service directly, otherwise the image only updates on a full
   * page reload.
   */
  it('refetches over the network even when the venue is already cached', async () => {
    mockedGetAllVenues.mockResolvedValueOnce({
      status: 200,
      data: [buildVenue()],
    } as never);
    mockedPutVenuePhotoById.mockResolvedValueOnce({ status: 204 } as never);
    mockedGetVenueById.mockResolvedValueOnce({
      status: 200,
      data: buildVenue({ photoUrl: 'https://cdn.test/venue/new.jpg' }),
    } as never);

    const { result } = renderHook(() => useVenue(), { wrapper });
    await act(async () => {
      await result.current.getAllVenues();
    });
    await waitFor(() => expect(result.current.venues).toHaveLength(1));

    const file = new File(['x'], 'court.jpg', { type: 'image/jpeg' });
    const returned = await act(() =>
      result.current.putVenuePhotoById(VENUE_ID, file)
    );

    expect(mockedGetVenueById).toHaveBeenCalledWith(VENUE_ID);
    expect(returned).toEqual(
      expect.objectContaining({ photoUrl: 'https://cdn.test/venue/new.jpg' })
    );
    expect(result.current.venue?.photoUrl).toBe(
      'https://cdn.test/venue/new.jpg'
    );
  });
});
