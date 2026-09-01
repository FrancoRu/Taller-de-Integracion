import { afterEach, describe, expect, it, vi } from 'vitest';
import { geocodeAddress } from './geocoding';

const mockFetch = (response: Partial<Response> & { jsonBody?: unknown }) => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue({
      ok: response.ok ?? true,
      json: () => Promise.resolve(response.jsonBody ?? []),
    })
  );
};

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('geocodeAddress', () => {
  it('returns the first result\'s coordinates', async () => {
    mockFetch({ jsonBody: [{ lat: '-34.6037', lon: '-58.3816' }] });

    const result = await geocodeAddress('Av. Corrientes 1000, CABA');

    expect(result).toEqual({ latitude: -34.6037, longitude: -58.3816 });
  });

  it('returns null for an empty address without calling the network', async () => {
    const fetchSpy = vi.fn();
    vi.stubGlobal('fetch', fetchSpy);

    const result = await geocodeAddress('   ');

    expect(result).toBeNull();
    expect(fetchSpy).not.toHaveBeenCalled();
  });

  it('returns null when the search yields no results', async () => {
    mockFetch({ jsonBody: [] });

    const result = await geocodeAddress('an address that does not exist anywhere');

    expect(result).toBeNull();
  });

  it('returns null when the request fails', async () => {
    mockFetch({ ok: false });

    const result = await geocodeAddress('some address');

    expect(result).toBeNull();
  });

  it('returns null when the network throws', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network down')));

    const result = await geocodeAddress('some address');

    expect(result).toBeNull();
  });
});
