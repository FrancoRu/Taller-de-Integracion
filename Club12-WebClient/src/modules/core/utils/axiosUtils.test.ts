import { AxiosError } from 'axios';
import axios from 'axios';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sendDelete } from './axiosUtils';

const originalLocation = window.location;

const mockAssign = (): ReturnType<typeof vi.fn> => {
  const assignSpy = vi.fn();
  Object.defineProperty(window, 'location', {
    configurable: true,
    value: { ...originalLocation, assign: assignSpy },
  });
  return assignSpy;
};

vi.mock('axios', async importOriginal => {
  const actual = await importOriginal<typeof import('axios')>();
  return {
    ...actual,
    default: {
      ...actual.default,
      request: vi.fn(),
    },
  };
});

const buildUnauthorizedError = (hasAuthHeader: boolean): AxiosError =>
  ({
    isAxiosError: true,
    name: 'AxiosError',
    message: 'Request failed with status code 401',
    config: {
      headers: hasAuthHeader ? { Authorization: 'Bearer expired-token' } : {},
    },
    response: {
      status: 401,
      data: {},
      statusText: 'Unauthorized',
      headers: {},
      config: { headers: {} },
    },
    toJSON: () => ({}),
  }) as unknown as AxiosError;

describe('axiosUtils invalid-token redirect', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: originalLocation,
    });
  });

  it('redirects to /token-invalido when a 401 error carries an Authorization header', async () => {
    const assignSpy = mockAssign();
    vi.mocked(axios.request).mockRejectedValueOnce(
      buildUnauthorizedError(true)
    );

    await expect(sendDelete('divisions/123')).rejects.toBeTruthy();

    expect(assignSpy).toHaveBeenCalledWith('/token-invalido');
  });

  it('does NOT redirect when a 401 error carries no Authorization header (and is not a refresh-token request)', async () => {
    const assignSpy = mockAssign();
    vi.mocked(axios.request).mockRejectedValueOnce(
      buildUnauthorizedError(false)
    );

    await expect(sendDelete('divisions/123')).rejects.toBeTruthy();

    expect(assignSpy).not.toHaveBeenCalled();
  });
});
