import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { StageType } from '@/modules/stage/type/stage';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { ITournamentStructureResponse } from '@/modules/tournament/type/tournament.d';
import DivisionFormatSection from './DivisionFormatSection';

vi.mock('@/modules/tournament/hook/tournament.hook');

const mockedUseTournament = vi.mocked(useTournament);

const groupStage = (name: string): ITournamentStructureResponse['divisions'][number]['stages'][number] => ({
  name,
  bracketName: null,
  stageType: StageType.Group,
  isElimination: false,
  order: 0,
  bestOf: 1,
  roundRobinLegs: 1,
});

const cupStages = (bracketName: string) => [
  {
    name: `Semifinal ${bracketName}`,
    bracketName,
    stageType: StageType.SemiFinal,
    isElimination: true,
    order: 1,
    bestOf: 1,
    roundRobinLegs: 1,
  },
  {
    name: `Final ${bracketName}`,
    bracketName,
    stageType: StageType.Final,
    isElimination: true,
    order: 2,
    bestOf: 1,
    roundRobinLegs: 1,
  },
];

const setup = (structure: ITournamentStructureResponse | undefined) => {
  mockedUseTournament.mockReturnValue({
    getStructure: vi.fn().mockResolvedValue(structure),
  } as unknown as ReturnType<typeof useTournament>);
};

describe('DivisionFormatSection', () => {
  it('shows the text format tree and a bracket diagram for a zone with sub-groups and a cup', async () => {
    setup({
      name: 'Apertura 2026',
      category: TournamentCategory.Masculine,
      divisions: [
        {
          name: 'Zona A',
          isCrossDivisionCup: false,
          pointsForWin: 2,
          pointsForLoss: 1,
          qualifiersPerGroup: 1,
          playoffMappings: [{ id: 'm1', fromPosition: 1, toPosition: 4, destination: 'Copa Oro' } as never],
          stages: [
            groupStage('Grupo A'),
            groupStage('Grupo B'),
            ...cupStages('Copa Oro'),
          ],
        },
      ],
    });

    render(<DivisionFormatSection tournamentIdOrSlug="t-1" divisionName="Zona A" />);

    expect(await screen.findByText('Formato')).toBeInTheDocument();
    expect(screen.getAllByText(/Grupo A/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Grupo B/).length).toBeGreaterThan(0);

    expect(await screen.findByText('Diagrama')).toBeInTheDocument();
    expect(screen.getByText('Copa Oro')).toBeInTheDocument();
  });

  it('shows only the group boxes, no bracket, when the zone has no cup', async () => {
    setup({
      name: 'Liga Solo Grupos',
      category: TournamentCategory.Masculine,
      divisions: [
        {
          name: 'Zona B',
          isCrossDivisionCup: false,
          pointsForWin: 2,
          pointsForLoss: 1,
          qualifiersPerGroup: 1,
          playoffMappings: [],
          stages: [groupStage('Fase de Grupos')],
        },
      ],
    });

    render(<DivisionFormatSection tournamentIdOrSlug="t-1" divisionName="Zona B" />);

    await screen.findByText('Formato');
    expect(screen.queryByText('Diagrama')).not.toBeInTheDocument();
  });

  it('skips the group-phase boxes for a groupless (playoffs-only) zone', async () => {
    setup({
      name: 'Torneo Playoffs',
      category: TournamentCategory.Masculine,
      divisions: [
        {
          name: 'Zona C',
          isCrossDivisionCup: false,
          pointsForWin: 2,
          pointsForLoss: 1,
          qualifiersPerGroup: 1,
          playoffMappings: [{ id: 'm2', fromPosition: 1, toPosition: 2, destination: 'Copa Única' } as never],
          stages: cupStages('Copa Única'),
        },
      ],
    });

    render(<DivisionFormatSection tournamentIdOrSlug="t-1" divisionName="Zona C" />);

    expect(await screen.findByText('Copa Única')).toBeInTheDocument();
    expect(screen.queryByText('Fase de grupos')).not.toBeInTheDocument();
  });

  it('shows a not-found message when no division in the structure matches the given name', async () => {
    setup({
      name: 'Apertura 2026',
      category: TournamentCategory.Masculine,
      divisions: [],
    });

    render(<DivisionFormatSection tournamentIdOrSlug="t-1" divisionName="Zona Inexistente" />);

    expect(
      await screen.findByText('No fue posible determinar el formato de esta división.')
    ).toBeInTheDocument();
  });

  it('shows the not-found message when the structure fetch itself fails', async () => {
    setup(undefined);

    render(<DivisionFormatSection tournamentIdOrSlug="t-1" divisionName="Zona A" />);

    await waitFor(() =>
      expect(
        screen.getByText('No fue posible determinar el formato de esta división.')
      ).toBeInTheDocument()
    );
  });
});
