import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import HabilitacionBadge from '@/views/medicalRecord/HabilitacionBadge';
import { MedicalRecordStatus } from '@/modules/core/enum/medicalRecord/medicalRecordStatus';

describe('HabilitacionBadge', () => {
  it('renders "Habilitado" when the player is habilitado (approved)', () => {
    render(
      <HabilitacionBadge
        isHabilitado
        status={MedicalRecordStatus.Approved}
      />
    );
    expect(screen.getByText('Habilitado')).toBeInTheDocument();
  });

  it('renders "No habilitado" for a pending record', () => {
    render(
      <HabilitacionBadge
        isHabilitado={false}
        status={MedicalRecordStatus.Pending}
      />
    );
    expect(screen.getByText('No habilitado')).toBeInTheDocument();
  });

  it('renders "No habilitado" for a rejected record', () => {
    render(
      <HabilitacionBadge
        isHabilitado={false}
        status={MedicalRecordStatus.Rejected}
      />
    );
    expect(screen.getByText('No habilitado')).toBeInTheDocument();
  });

  it('renders "No habilitado" when there is no record at all', () => {
    render(<HabilitacionBadge />);
    expect(screen.getByText('No habilitado')).toBeInTheDocument();
  });
});
