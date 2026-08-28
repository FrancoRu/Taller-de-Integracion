import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import BackupsTable from '@/views/panel/components/BackupsTable';
import type { IBackupRecordResponse } from '@/modules/backup/type/backup';

vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
  },
}));

import Swal from 'sweetalert2';

const mockedSwalFire = vi.mocked(Swal.fire);

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

const buildRecord = (
  overrides: Partial<IBackupRecordResponse> = {}
): IBackupRecordResponse => ({
  id: 'guid-1-aaaa-bbbb-cccc',
  createdAt: '2026-08-19T10:00:00Z',
  sizeBytes: 2048,
  origin: 'Manual',
  storagePath: 'backup-1.sql',
  ...overrides,
});

beforeEach(() => {
  stubLayoutDimensions();
  mockedSwalFire.mockResolvedValue({
    isConfirmed: true,
    isDenied: false,
    isDismissed: false,
  } as Awaited<ReturnType<typeof Swal.fire>>);
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('BackupsTable — columns', () => {
  it('renders Fecha/Peso/Forma de creación/Acciones for each row', () => {
    render(
      <BackupsTable
        backups={[buildRecord()]}
        loading={false}
        onDelete={vi.fn()}
        onRestore={vi.fn()}
      />
    );

    expect(screen.getByText('Fecha')).toBeInTheDocument();
    expect(screen.getByText('Peso')).toBeInTheDocument();
    expect(screen.getByText('Forma de creación')).toBeInTheDocument();
    expect(screen.getByText('Acciones')).toBeInTheDocument();
    expect(screen.getByText('2 KB')).toBeInTheDocument();
    expect(screen.getByText('Manual')).toBeInTheDocument();
  });

  it('shows the "Programado" label for Job-origin records', () => {
    render(
      <BackupsTable
        backups={[buildRecord({ origin: 'Job' })]}
        loading={false}
        onDelete={vi.fn()}
        onRestore={vi.fn()}
      />
    );

    expect(screen.getByText('Programado')).toBeInTheDocument();
  });

  it('shows a trash icon and a restore icon in the Actions column', async () => {
    render(
      <BackupsTable
        backups={[buildRecord()]}
        loading={false}
        onDelete={vi.fn()}
        onRestore={vi.fn()}
      />
    );

    expect(await screen.findByTestId('DeleteIcon')).toBeInTheDocument();
    expect(await screen.findByTestId('RestoreIcon')).toBeInTheDocument();
  });
});

describe('BackupsTable — delete confirmation', () => {
  it('does not call onDelete when the confirmation is declined', async () => {
    mockedSwalFire.mockResolvedValueOnce({
      isConfirmed: false,
      isDenied: false,
      isDismissed: true,
    } as Awaited<ReturnType<typeof Swal.fire>>);
    const onDelete = vi.fn();

    render(
      <BackupsTable
        backups={[buildRecord()]}
        loading={false}
        onDelete={onDelete}
        onRestore={vi.fn()}
      />
    );

    const deleteIcon = await screen.findByTestId('DeleteIcon');
    fireEvent.click(deleteIcon.closest('button') as HTMLButtonElement);

    await waitFor(() => expect(mockedSwalFire).toHaveBeenCalledTimes(1));
    expect(onDelete).not.toHaveBeenCalled();
  });

  it('calls onDelete with the row when the confirmation is accepted', async () => {
    const record = buildRecord();
    const onDelete = vi.fn();

    render(
      <BackupsTable
        backups={[record]}
        loading={false}
        onDelete={onDelete}
        onRestore={vi.fn()}
      />
    );

    const deleteIcon = await screen.findByTestId('DeleteIcon');
    fireEvent.click(deleteIcon.closest('button') as HTMLButtonElement);

    await waitFor(() => expect(onDelete).toHaveBeenCalledWith(record));
  });
});

describe('BackupsTable — restore confirmation', () => {
  it('does not call onRestore when the confirmation is declined', async () => {
    mockedSwalFire.mockResolvedValueOnce({
      isConfirmed: false,
      isDenied: false,
      isDismissed: true,
    } as Awaited<ReturnType<typeof Swal.fire>>);
    const onRestore = vi.fn();

    render(
      <BackupsTable
        backups={[buildRecord()]}
        loading={false}
        onDelete={vi.fn()}
        onRestore={onRestore}
      />
    );

    const restoreIcon = await screen.findByTestId('RestoreIcon');
    fireEvent.click(restoreIcon.closest('button') as HTMLButtonElement);

    await waitFor(() => expect(mockedSwalFire).toHaveBeenCalledTimes(1));
    expect(onRestore).not.toHaveBeenCalled();
  });

  it('calls onRestore with the row when the confirmation is accepted', async () => {
    const record = buildRecord();
    const onRestore = vi.fn();

    render(
      <BackupsTable
        backups={[record]}
        loading={false}
        onDelete={vi.fn()}
        onRestore={onRestore}
      />
    );

    const restoreIcon = await screen.findByTestId('RestoreIcon');
    fireEvent.click(restoreIcon.closest('button') as HTMLButtonElement);

    await waitFor(() => expect(onRestore).toHaveBeenCalledWith(record));
  });
});
