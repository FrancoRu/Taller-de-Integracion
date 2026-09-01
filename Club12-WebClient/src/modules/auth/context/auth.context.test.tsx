import { act, renderHook } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import Swal from 'sweetalert2';
import { ErrorProvider } from '@/modules/error/context/error.context';
import { AuthProvider } from '@/modules/auth/context/auth.context';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { authService } from '@/modules/auth/service/auth.service';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';

vi.mock('@/modules/auth/service/auth.service');
vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
    getContainer: vi.fn().mockReturnValue(null),
  },
}));

const mockedLoginRequest = vi.mocked(authService.loginRequest);
const mockedSwalFire = vi.mocked(Swal.fire);

const wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={new QueryClient()}>
    <ErrorProvider>
      <AuthProvider>{children}</AuthProvider>
    </ErrorProvider>
  </QueryClientProvider>
);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('AuthProvider — signIn failure', () => {
  it('shows exactly one Spanish toast and resolves false, never the raw backend error', async () => {
    mockedLoginRequest.mockRejectedValueOnce(
      new Error('Invalid credentials.')
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    let success: boolean | undefined;
    await act(async () => {
      success = await result.current.signIn({
        email: 'wrong@club12.test',
        password: 'wrong',
      });
    });

    expect(success).toBe(false);
    expect(mockedSwalFire).toHaveBeenCalledTimes(1);
    expect(mockedSwalFire).toHaveBeenCalledWith(
      expect.objectContaining({
        icon: 'error',
        title: ERROR_MESSAGES.LOGIN_FAILED,
      })
    );
  });
});
