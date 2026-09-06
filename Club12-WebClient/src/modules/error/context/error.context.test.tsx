import { act, renderHook } from '@testing-library/react';
import { AxiosError } from 'axios';
import type { ReactNode } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import Swal from 'sweetalert2';
import { ErrorProvider } from '@/modules/error/context/error.context';
import { useError } from '@/modules/error/hooks/error.hock';

vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
    getContainer: vi.fn().mockReturnValue(null),
  },
}));

const mockedSwalFire = vi.mocked(Swal.fire);

const wrapper = ({ children }: { children: ReactNode }) => (
  <ErrorProvider>{children}</ErrorProvider>
);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('ErrorProvider — setMessage toast duration', () => {
  /**
   * Regression test for a "silent failure" bug: a rejected mutation (e.g. a
   * match edit or a score-sheet submit) DID receive a well-formed backend
   * error and DID render a toast, but the toast auto-dismissed after 1500ms
   * with no button — the same fast timer used for a quick success
   * confirmation. Backend validation messages are often a full sentence
   * (roster/eligibility rules, "cannot edit a started match", etc.) that
   * can't be read that fast, so in practice it looked exactly like no
   * feedback was shown at all. Errors must instead stay up until the user
   * dismisses them.
   */
  it('shows an error toast that waits for the user to dismiss it, not a 1500ms timer', () => {
    const { result } = renderHook(() => useError(), { wrapper });

    act(() => {
      result.current.setMessage(400, ['No se puede editar un partido que ya arrancó o finalizó.']);
    });

    expect(mockedSwalFire).toHaveBeenCalledTimes(1);
    const options = mockedSwalFire.mock.calls[0][0] as unknown as Record<string, unknown>;
    expect(options.icon).toBe('error');
    expect(options.showConfirmButton).toBe(true);
    expect(options.timer).toBeUndefined();
  });

  it('keeps the fast auto-dismiss timer for a success message', () => {
    const { result } = renderHook(() => useError(), { wrapper });

    act(() => {
      result.current.setMessage(200, ['Partido actualizado']);
    });

    expect(mockedSwalFire).toHaveBeenCalledTimes(1);
    const options = mockedSwalFire.mock.calls[0][0] as unknown as Record<string, unknown>;
    expect(options.icon).toBe('success');
    expect(options.showConfirmButton).toBe(false);
    expect(options.timer).toBe(1500);
  });

  it('setError (used for unknown/axios errors) also surfaces a dismiss-required toast', () => {
    const { result } = renderHook(() => useError(), { wrapper });

    const error = new AxiosError('Request failed with status code 400');
    Object.assign(error, {
      response: {
        status: 400,
        data: { detail: 'No se puede editar un partido que ya arrancó o finalizó.' },
      },
    });

    act(() => {
      result.current.setError(error);
    });

    expect(mockedSwalFire).toHaveBeenCalledTimes(1);
    const options = mockedSwalFire.mock.calls[0][0] as unknown as Record<string, unknown>;
    expect(options.showConfirmButton).toBe(true);
    expect(options.timer).toBeUndefined();
  });
});
