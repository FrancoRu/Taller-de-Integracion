import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import AuditLogsPage from '@/views/panel/AuditLogsPage';
import { useAuditLog } from '@/modules/auditLog/hook/auditLog.hook';
import type { IAuditLogResponse } from '@/modules/auditLog/type/auditLog';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/auditLog/hook/auditLog.hook');

const stubLayoutDimensions = () => {
  Object.defineProperties(window.HTMLElement.prototype, {
    offsetWidth: { configurable: true, get: () => 1000 },
    offsetHeight: { configurable: true, get: () => 1000 },
    clientWidth: { configurable: true, get: () => 1000 },
    clientHeight: { configurable: true, get: () => 1000 },
  });
  window.HTMLElement.prototype.getBoundingClientRect = () =>
    ({
      width: 1000,
      height: 1000,
      top: 0,
      left: 0,
      right: 1000,
      bottom: 1000,
      x: 0,
      y: 0,
      toJSON() {},
    }) as DOMRect;
};

if (!window.ResizeObserver) {
  window.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  } as unknown as typeof ResizeObserver;
}
stubLayoutDimensions();

const mockedUseAuditLog = vi.mocked(useAuditLog);

const entries: IAuditLogResponse[] = [
  {
    id: 'guid-1-aaaa-bbbb-cccc' as unknown as GUID,
    action: 'TournamentStatusChange',
    actor: 'owner@club12.com',
    targetType: 'Tournament',
    targetId: 'apertura-2026',
    detail: 'Programado → Inscripción abierta',
    timestamp: '2026-08-20T13:00:00Z',
  },
];

describe('AuditLogsPage (HU-101)', () => {
  it('renders the audit entries returned by the service', async () => {
    const getAuditLogs = vi.fn().mockResolvedValue({
      items: entries,
      page: 1,
      pageSize: 10,
      totalCount: 1,
    });
    mockedUseAuditLog.mockReturnValue({ getAuditLogs });

    render(<AuditLogsPage />);

    await waitFor(() => expect(getAuditLogs).toHaveBeenCalled());
    expect(await screen.findByText('owner@club12.com')).toBeInTheDocument();
    expect(
      await screen.findByText('Cambio de estado de torneo')
    ).toBeInTheDocument();
    expect(
      await screen.findByText('Programado → Inscripción abierta')
    ).toBeInTheDocument();
  });

  it('shows the captured target name instead of the raw id, when present', async () => {
    const getAuditLogs = vi.fn().mockResolvedValue({
      items: [{ ...entries[0], targetName: 'Torneo Apertura 2026' }],
      page: 1,
      pageSize: 10,
      totalCount: 1,
    });
    mockedUseAuditLog.mockReturnValue({ getAuditLogs });

    render(<AuditLogsPage />);

    expect(
      await screen.findByText('Tournament: Torneo Apertura 2026')
    ).toBeInTheDocument();
    expect(screen.queryByText(/apertura-2026/)).not.toBeInTheDocument();
  });

  it('falls back to type + raw id for entries with no captured target name', async () => {
    const getAuditLogs = vi.fn().mockResolvedValue({
      items: entries,
      page: 1,
      pageSize: 10,
      totalCount: 1,
    });
    mockedUseAuditLog.mockReturnValue({ getAuditLogs });

    render(<AuditLogsPage />);

    expect(
      await screen.findByText('Tournament: apertura-2026')
    ).toBeInTheDocument();
  });
});
