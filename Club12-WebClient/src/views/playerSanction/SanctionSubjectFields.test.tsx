import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { SanctionSubjectType } from '@/modules/playerSanction/type/playerSanction.d';
import SanctionSubjectFields from '@/views/playerSanction/SanctionSubjectFields';

const guid = (value: string) => value as GUID;

const baseProps = {
  teamId: '' as GUID | '',
  playerId: '' as GUID | '',
  staffName: '',
  teamOptions: [{ id: guid('team-1'), name: 'Club 12' }],
  playerOptions: [{ id: guid('player-1'), fullName: 'Ana Gómez' }],
  onSubjectTypeChange: vi.fn(),
  onTeamChange: vi.fn(),
  onPlayerChange: vi.fn(),
  onStaffNameChange: vi.fn(),
};

const renderFields = (subjectType: SanctionSubjectType) =>
  render(<SanctionSubjectFields {...baseProps} subjectType={subjectType} />);

describe('SanctionSubjectFields (HU-77)', () => {
  it('shows the team and player pickers for a player subject', () => {
    renderFields('Player');

    expect(
      screen.getByRole('combobox', { name: /equipo/i })
    ).toBeInTheDocument();
    expect(
      screen.getByRole('combobox', { name: /jugador/i })
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('textbox', { name: /nombre del staff/i })
    ).not.toBeInTheDocument();
  });

  it('switches to only the team picker for a team subject', () => {
    renderFields('Team');

    expect(
      screen.getByRole('combobox', { name: /equipo/i })
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('combobox', { name: /jugador/i })
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('textbox', { name: /nombre del staff/i })
    ).not.toBeInTheDocument();
  });

  it('switches to a free-text staff name for a staff subject', () => {
    renderFields('Staff');

    expect(
      screen.getByRole('textbox', { name: /nombre del staff/i })
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('combobox', { name: /equipo/i })
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('combobox', { name: /jugador/i })
    ).not.toBeInTheDocument();
  });

  it('reports staff name edits through onStaffNameChange', () => {
    const onStaffNameChange = vi.fn();
    render(
      <SanctionSubjectFields
        {...baseProps}
        subjectType="Staff"
        onStaffNameChange={onStaffNameChange}
      />
    );

    fireEvent.change(
      screen.getByRole('textbox', { name: /nombre del staff/i }),
      { target: { value: 'Coordinador X' } }
    );

    expect(onStaffNameChange).toHaveBeenCalledWith('Coordinador X');
  });
});
