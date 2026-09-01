import { AxiosError } from 'axios';
import axios from 'axios';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sendDelete, sendGet, sendPost } from './axiosUtils';
import { getActiveRequestCount } from './requestActivity';

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

const buildNotFoundError = (): AxiosError =>
  ({
    isAxiosError: true,
    name: 'AxiosError',
    message: 'Request failed with status code 404',
    config: { headers: {} },
    response: {
      status: 404,
      data: {
        title: 'Not Found: The specified resource could not be found.',
        detail: 'Division with id 123 not found.',
        status: 404,
      },
      statusText: 'Not Found',
      headers: {},
      config: { headers: {} },
    },
    toJSON: () => ({}),
  }) as unknown as AxiosError;

describe('sendGet error pipeline', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: originalLocation,
    });
  });

  it('redirects to /token-invalido when a GET request gets a 401 with an Authorization header', async () => {
    const assignSpy = mockAssign();
    vi.mocked(axios.request).mockRejectedValueOnce(
      buildUnauthorizedError(true)
    );

    await expect(sendGet('divisions/123')).rejects.toBeTruthy();

    expect(assignSpy).toHaveBeenCalledWith('/token-invalido');
  });

  it('rejects with the same error shape as other verbs when a GET request gets a 404', async () => {
    const notFoundError = buildNotFoundError();
    vi.mocked(axios.request).mockRejectedValueOnce(notFoundError);

    await expect(sendGet('divisions/123')).rejects.toBe(notFoundError);
  });
});

describe('mutating-request activity tracking (drives GlobalLoadingOverlay)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('increments the active count while a POST is in flight, then releases it on success', async () => {
    let resolveRequest: (value: unknown) => void = () => {};
    const pending = new Promise(resolve => {
      resolveRequest = resolve;
    });
    vi.mocked(axios.request).mockReturnValueOnce(pending as never);

    expect(getActiveRequestCount()).toBe(0);
    const requestPromise = sendPost('teams', { name: 'River' });
    await Promise.resolve();
    expect(getActiveRequestCount()).toBe(1);

    resolveRequest({ data: {}, status: 201, statusText: 'Created', headers: {}, config: {} });
    await requestPromise;
    expect(getActiveRequestCount()).toBe(0);
  });

  it('releases the active count even when the mutating request fails', async () => {
    vi.mocked(axios.request).mockRejectedValueOnce(buildNotFoundError());

    await expect(sendPost('teams', { name: 'River' })).rejects.toBeTruthy();
    expect(getActiveRequestCount()).toBe(0);
  });

  it('does NOT affect the active count for a GET (page data keeps its own skeleton loading)', async () => {
    vi.mocked(axios.request).mockResolvedValueOnce({
      data: {},
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {},
    });

    await sendGet('teams');
    expect(getActiveRequestCount()).toBe(0);
  });
});
