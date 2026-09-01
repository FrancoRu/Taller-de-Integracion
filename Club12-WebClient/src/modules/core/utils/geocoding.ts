/**
 * Looks up an address's coordinates via OpenStreetMap's free Nominatim
 * search API (no API key required). Callers must debounce their own calls
 * (e.g. on a pause in typing) rather than firing on every keystroke, per
 * Nominatim's usage policy (https://operations.osmfoundation.org/policies/nominatim/).
 * @param address The free-text address to look up.
 * @returns The first match's coordinates, or null if nothing was found or the
 * lookup failed.
 */
export const geocodeAddress = async (
  address: string
): Promise<{ latitude: number; longitude: number } | null> => {
  const trimmed = address.trim();
  if (!trimmed) {
    return null;
  }

  try {
    const url = `https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q=${encodeURIComponent(trimmed)}`;
    const response = await fetch(url, {
      headers: { Accept: 'application/json' },
    });

    if (!response.ok) {
      return null;
    }

    const results: Array<{ lat: string; lon: string }> = await response.json();
    const [first] = results;
    if (!first) {
      return null;
    }

    const latitude = Number(first.lat);
    const longitude = Number(first.lon);
    if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
      return null;
    }

    return { latitude, longitude };
  } catch {
    return null;
  }
};
