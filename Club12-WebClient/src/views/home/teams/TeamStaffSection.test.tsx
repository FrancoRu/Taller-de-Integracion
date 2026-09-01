import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import TeamStaffSection from '@/views/home/teams/TeamStaffSection';
import { ITeamStaffResponse } from '@/modules/teamStaff/type/teamStaff';
import type { GUID } from '@/modules/core/types/types';

const buildStaff = (
  overrides: Partial<ITeamStaffResponse> = {}
): ITeamStaffResponse => ({
  id: 'staff-1' as unknown as GUID,
  teamId: 'team-1' as unknown as GUID,
  tournamentId: 'tournament-1' as unknown as GUID,
  fullName: 'Juan Pérez',
  role: 'Coach',
  dateCreated: '2026-01-01T00:00:00Z',
  ...overrides,
});

describe('TeamStaffSection', () => {
  it('renders nothing when there is no staff', () => {
    const { container } = render(<TeamStaffSection staff={[]} />);

    expect(container).toBeEmptyDOMElement();
  });

  it('renders each staff member with their Spanish role label', () => {
    render(
      <TeamStaffSection
        staff={[
          buildStaff({ fullName: 'Juan Pérez', role: 'Coach' }),
          buildStaff({
            id: 'staff-2' as unknown as GUID,
            fullName: 'María López',
            role: 'AssistantCoach',
          }),
        ]}
      />
    );

    expect(screen.getByText('Cuerpo técnico')).toBeInTheDocument();
    expect(screen.getByText('Juan Pérez')).toBeInTheDocument();
    expect(screen.getByText('DT')).toBeInTheDocument();
    expect(screen.getByText('María López')).toBeInTheDocument();
    expect(screen.getByText('Asistente')).toBeInTheDocument();
  });
});
