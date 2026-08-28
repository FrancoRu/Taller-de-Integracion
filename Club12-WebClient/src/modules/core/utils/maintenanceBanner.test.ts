import { AxiosError } from 'axios';
import axios from 'axios';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sendGet } from '@/modules/core/utils/axiosUtils';
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import {
  dismissMaintenanceBanner,
  getMaintenanceBannerSnapshot,
  subscribeMaintenanceBanner,
} from '@/modules/core/utils/maintenanceBanner';

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

const build503Error = (): AxiosError =>
  ({
    isAxiosError: true,
    name: 'AxiosError',
    message: 'Request failed with status code 503',
    config: { headers: {} },
    response: {
      status: HttpStatus.ServiceUnavailable,
      data: { message: 'La base de datos está en mantenimiento.' },
      statusText: 'Service Unavailable',
      headers: {},
      config: { headers: {} },
    },
    toJSON: () => ({}),
  }) as unknown as AxiosError;

beforeEach(() => {
  vi.clearAllMocks();
  dismissMaintenanceBanner();
});

afterEach(() => {
  dismissMaintenanceBanner();
});

describe('maintenance banner — registered against the axiosUtils handler registry', () => {
  it('starts inactive', () => {
    expect(getMaintenanceBannerSnapshot()).toBe(false);
  });

  it('flips active when any request receives a 503, via onStatusCode(HttpStatus.ServiceUnavailable, ...)', async () => {
    vi.mocked(axios.request).mockRejectedValueOnce(build503Error());

    await expect(sendGet('backups')).rejects.toBeTruthy();

    expect(getMaintenanceBannerSnapshot()).toBe(true);
  });

  it('notifies subscribers when the banner flips active', async () => {
    vi.mocked(axios.request).mockRejectedValueOnce(build503Error());
    let notified = false;
    const unsubscribe = subscribeMaintenanceBanner(() => {
      notified = true;
    });

    await expect(sendGet('backups')).rejects.toBeTruthy();

    expect(notified).toBe(true);
    unsubscribe();
  });

  it('does not flip the banner for an unrelated status code (e.g. 404)', async () => {
    const notFoundError = {
      isAxiosError: true,
      name: 'AxiosError',
      message: 'Request failed with status code 404',
      config: { headers: {} },
      response: {
        status: HttpStatus.NotFound,
        data: {},
        statusText: 'Not Found',
        headers: {},
        config: { headers: {} },
      },
      toJSON: () => ({}),
    } as unknown as AxiosError;
    vi.mocked(axios.request).mockRejectedValueOnce(notFoundError);

    await expect(sendGet('backups')).rejects.toBeTruthy();

    expect(getMaintenanceBannerSnapshot()).toBe(false);
  });

  it('dismissMaintenanceBanner resets the banner to inactive and notifies subscribers', async () => {
    vi.mocked(axios.request).mockRejectedValueOnce(build503Error());
    await expect(sendGet('backups')).rejects.toBeTruthy();
    expect(getMaintenanceBannerSnapshot()).toBe(true);

    let notified = false;
    const unsubscribe = subscribeMaintenanceBanner(() => {
      notified = true;
    });

    dismissMaintenanceBanner();

    expect(getMaintenanceBannerSnapshot()).toBe(false);
    expect(notified).toBe(true);
    unsubscribe();
  });
});
