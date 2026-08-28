import { afterEach, describe, expect, it, vi } from 'vitest';
import { authService } from '@/modules/auth/service/auth.service';
import { sendPost } from '@/modules/core/utils/axiosUtils';

vi.mock('@/modules/core/utils/axiosUtils', () => ({
  sendPost: vi.fn(() => Promise.resolve({ status: 200, data: {} })),
}));

const sendPostMock = vi.mocked(sendPost);

describe('authService magic-link endpoints (HU-09/HU-10)', () => {
  afterEach(() => {
    sendPostMock.mockClear();
  });

  it('inviteRequest posts email + role to auth/invite', async () => {
    await authService.inviteRequest({
      email: 'nuevo@club12.com',
      role: 'ADMIN',
    });

    expect(sendPostMock).toHaveBeenCalledWith('auth/invite', {
      email: 'nuevo@club12.com',
      role: 'ADMIN',
    });
  });

  it('activateRequest posts email + token + newPassword to auth/activate', async () => {
    await authService.activateRequest({
      email: 'nuevo@club12.com',
      token: 'activation-token',
      newPassword: 'Str0ng!Pass',
    });

    expect(sendPostMock).toHaveBeenCalledWith('auth/activate', {
      email: 'nuevo@club12.com',
      token: 'activation-token',
      newPassword: 'Str0ng!Pass',
    });
  });

  it('requestPasswordResetRequest posts email to auth/password-reset/request', async () => {
    await authService.requestPasswordResetRequest({
      email: 'olvide@club12.com',
    });

    expect(sendPostMock).toHaveBeenCalledWith('auth/password-reset/request', {
      email: 'olvide@club12.com',
    });
  });
});
