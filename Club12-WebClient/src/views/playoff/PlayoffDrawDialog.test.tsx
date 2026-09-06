import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import PlayoffDrawDialog from '@/views/playoff/PlayoffDrawDialog';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { DrawMode } from '@/modules/stage/type/stage';
import type { GUID } from '@/modules/core/types/types';
import type { ITeamResponse } from '@/modules/team/type/team.d';
import type { IStageContextProps } from '@/modules/stage/type/stage';

vi.mock('@/modules/stage/hook/stage.hook');
vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn(), getContainer: vi.fn().mockReturnValue(null) },
}));

import Swal from 'sweetalert2';

const mockedUseStage = vi.mocked(useStage);
const mockedSwalFire = vi.mocked(Swal.fire);

const gid = (value: string): GUID => value as unknown as GUID;

const buildTeam = (id: string, name: string): ITeamResponse => ({
  id: gid(id),
  name,
  slug: name.toLowerCase(),
  threeLetterCode: name.slice(0, 3).toUpperCase(),
  shirtColor: 'Rojo',
  logoUrl: '',
  players: [],
  tournamentId: gid('tournament-1'),
  tournamentName: null,
});

const STAGE_ID = gid('stage-final-1');

let previewDraw: Mock<IStageContextProps['previewDraw']>;
let commitDraw: Mock<IStageContextProps['commitDraw']>;

const setup = () => {
  previewDraw = vi.fn<IStageContextProps['previewDraw']>();
  commitDraw = vi.fn<IStageContextProps['commitDraw']>();
  commitDraw.mockResolvedValue(true);

  mockedUseStage.mockReturnValue({
    stage: null,
    stages: null,
    addStage: vi.fn(),
    putStageById: vi.fn(),
    getStagesByFilters: vi.fn(),
    getStageById: vi.fn(),
    deleteStagesById: vi.fn(),
    assignTeamsToStage: vi.fn(),
    unassignTeamsFromStage: vi.fn(),
    seedKnockoutStage: vi.fn(),
    previewDraw,
    commitDraw,
  } as IStageContextProps);
};

beforeEach(() => {
  setup();
  mockedSwalFire.mockResolvedValue({
    isConfirmed: true,
    isDenied: false,
    isDismissed: false,
  } as Awaited<ReturnType<typeof Swal.fire>>);
});

const roster = [buildTeam('t1', 'River'), buildTeam('t2', 'Boca'), buildTeam('t3', 'Colón')];

describe('PlayoffDrawDialog — random draw preview/confirm', () => {
  it('previews a random draw and holds the draw token', async () => {
    previewDraw.mockResolvedValueOnce({
      pairs: [
        { homeTeamId: gid('t1'), visitorTeamId: gid('t2') },
        { homeTeamId: gid('t3'), visitorTeamId: null },
      ],
      drawToken: 'token-1',
    });

    const user = userEvent.setup();
    render(
      <PlayoffDrawDialog
        open
        onClose={vi.fn()}
        stageId={STAGE_ID}
        roster={roster}
        onCommitted={vi.fn()}
      />
    );

    await user.click(screen.getByRole('button', { name: /sortear llave \(aleatorio\)/i }));

    await waitFor(() => expect(previewDraw).toHaveBeenCalledTimes(1));
    expect(previewDraw).toHaveBeenCalledWith(STAGE_ID, { mode: DrawMode.Random });

    expect(await screen.findByText(/River vs Boca/i)).toBeInTheDocument();
    expect(screen.getByText(/Colón vs BYE/i)).toBeInTheDocument();
  });

  it('re-previews (new token) on "Volver a sortear"', async () => {
    previewDraw
      .mockResolvedValueOnce({
        pairs: [{ homeTeamId: gid('t1'), visitorTeamId: gid('t2') }],
        drawToken: 'token-1',
      })
      .mockResolvedValueOnce({
        pairs: [{ homeTeamId: gid('t2'), visitorTeamId: gid('t1') }],
        drawToken: 'token-2',
      });

    const user = userEvent.setup();
    render(
      <PlayoffDrawDialog
        open
        onClose={vi.fn()}
        stageId={STAGE_ID}
        roster={roster}
        onCommitted={vi.fn()}
      />
    );

    await user.click(screen.getByRole('button', { name: /sortear llave \(aleatorio\)/i }));
    await screen.findByText(/River vs Boca/i);

    await user.click(screen.getByRole('button', { name: /volver a sortear/i }));

    await waitFor(() => expect(previewDraw).toHaveBeenCalledTimes(2));
    expect(await screen.findByText(/Boca vs River/i)).toBeInTheDocument();
  });

  it('confirms the commit with the previewed drawToken', async () => {
    previewDraw.mockResolvedValueOnce({
      pairs: [{ homeTeamId: gid('t1'), visitorTeamId: gid('t2') }],
      drawToken: 'token-1',
    });

    const onCommitted = vi.fn();
    const user = userEvent.setup();
    render(
      <PlayoffDrawDialog
        open
        onClose={vi.fn()}
        stageId={STAGE_ID}
        roster={roster}
        onCommitted={onCommitted}
      />
    );

    await user.click(screen.getByRole('button', { name: /sortear llave \(aleatorio\)/i }));
    await screen.findByText(/River vs Boca/i);
    await user.click(screen.getByRole('button', { name: /confirmar sorteo/i }));

    await waitFor(() => expect(commitDraw).toHaveBeenCalledTimes(1));
    expect(commitDraw).toHaveBeenCalledWith(STAGE_ID, {
      mode: DrawMode.Random,
      drawToken: 'token-1',
    });
    await waitFor(() => expect(onCommitted).toHaveBeenCalledTimes(1));
  });
});

describe('PlayoffDrawDialog — manual seeding', () => {
  it('submits { mode: Manual, manualOrder } without any random shuffle', async () => {
    const onCommitted = vi.fn();
    const user = userEvent.setup();
    render(
      <PlayoffDrawDialog
        open
        onClose={vi.fn()}
        stageId={STAGE_ID}
        roster={roster}
        onCommitted={onCommitted}
      />
    );

    await user.click(screen.getByRole('tab', { name: /manual/i }));
    await user.click(screen.getByRole('button', { name: /confirmar sorteo/i }));

    await waitFor(() => expect(commitDraw).toHaveBeenCalledTimes(1));
    expect(previewDraw).not.toHaveBeenCalled();
    expect(commitDraw).toHaveBeenCalledWith(STAGE_ID, {
      mode: DrawMode.Manual,
      manualOrder: [gid('t1'), gid('t2'), gid('t3')],
    });
    await waitFor(() => expect(onCommitted).toHaveBeenCalledTimes(1));
  });

  it('reordering with the down arrow changes the submitted manualOrder', async () => {
    const user = userEvent.setup();
    render(
      <PlayoffDrawDialog
        open
        onClose={vi.fn()}
        stageId={STAGE_ID}
        roster={roster}
        onCommitted={vi.fn()}
      />
    );

    await user.click(screen.getByRole('tab', { name: /manual/i }));
    await user.click(screen.getByRole('button', { name: /bajar river/i }));
    await user.click(screen.getByRole('button', { name: /confirmar sorteo/i }));

    await waitFor(() => expect(commitDraw).toHaveBeenCalledTimes(1));
    expect(commitDraw).toHaveBeenCalledWith(STAGE_ID, {
      mode: DrawMode.Manual,
      manualOrder: [gid('t2'), gid('t1'), gid('t3')],
    });
  });
});
