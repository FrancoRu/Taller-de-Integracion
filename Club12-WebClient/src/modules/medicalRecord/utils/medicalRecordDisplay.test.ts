import { describe, expect, it } from 'vitest';
import { MedicalRecordStatus } from '@/modules/core/enum/medicalRecord/medicalRecordStatus';
import {
  getHabilitacionColor,
  getHabilitacionLabel,
  getMedicalRecordDetail,
  resolveIsHabilitado,
} from './medicalRecordDisplay';

describe('medicalRecordDisplay', () => {
  it('resolves habilitado from the explicit flag first', () => {
    expect(resolveIsHabilitado(true, MedicalRecordStatus.Rejected)).toBe(true);
    expect(resolveIsHabilitado(false, MedicalRecordStatus.Approved)).toBe(false);
  });

  it('falls back to Approved status when the flag is missing', () => {
    expect(resolveIsHabilitado(undefined, MedicalRecordStatus.Approved)).toBe(
      true
    );
    expect(resolveIsHabilitado(undefined, MedicalRecordStatus.Pending)).toBe(
      false
    );
    expect(resolveIsHabilitado(null, null)).toBe(false);
  });

  it('labels habilitado vs not habilitado', () => {
    expect(getHabilitacionLabel(true)).toBe('Habilitado');
    expect(getHabilitacionLabel(false, MedicalRecordStatus.Pending)).toBe(
      'No habilitado'
    );
  });

  it('colors success / error / warning by status', () => {
    expect(getHabilitacionColor(true, MedicalRecordStatus.Approved)).toBe(
      'success'
    );
    expect(getHabilitacionColor(false, MedicalRecordStatus.Rejected)).toBe(
      'error'
    );
    expect(getHabilitacionColor(false, MedicalRecordStatus.Pending)).toBe(
      'warning'
    );
    expect(getHabilitacionColor(false, null)).toBe('warning');
  });

  it('describes the medical-record detail', () => {
    expect(getMedicalRecordDetail(null)).toBe('Sin ficha médica cargada');
    expect(getMedicalRecordDetail(MedicalRecordStatus.Approved)).toBe(
      'Ficha médica: Aprobada'
    );
    expect(getMedicalRecordDetail(MedicalRecordStatus.Rejected)).toBe(
      'Ficha médica: Rechazada'
    );
  });
});
