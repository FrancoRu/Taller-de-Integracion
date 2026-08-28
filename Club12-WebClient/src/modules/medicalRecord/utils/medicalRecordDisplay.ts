import { MedicalRecordStatus } from '@/modules/core/enum/medicalRecord/medicalRecordStatus';

/** MUI Chip color used by the habilitación / medical-record badges. */
export type HabilitacionColor = 'success' | 'warning' | 'error' | 'default';

export const HABILITADO_LABEL = 'Habilitado';
export const NO_HABILITADO_LABEL = 'No habilitado';

/** Spanish label for each raw medical-record status. */
export const MEDICAL_RECORD_STATUS_LABELS: Record<MedicalRecordStatus, string> =
  {
    [MedicalRecordStatus.Pending]: 'Pendiente',
    [MedicalRecordStatus.Approved]: 'Aprobada',
    [MedicalRecordStatus.Rejected]: 'Rechazada',
  };

/**
 * Resolves whether a player is habilitado from the roster/medical-record
 * signals. Prefers the explicit `isHabilitado` flag and falls back to the
 * status being Approved when the flag was not populated (HU-57).
 */
export const resolveIsHabilitado = (
  isHabilitado?: boolean | null,
  status?: MedicalRecordStatus | null
): boolean => isHabilitado ?? status === MedicalRecordStatus.Approved;

/** The badge label: "Habilitado" / "No habilitado" (HU-57/HU-62). */
export const getHabilitacionLabel = (
  isHabilitado?: boolean | null,
  status?: MedicalRecordStatus | null
): string =>
  resolveIsHabilitado(isHabilitado, status)
    ? HABILITADO_LABEL
    : NO_HABILITADO_LABEL;

/**
 * The badge color: success when habilitado, error when the record was
 * explicitly rejected, warning otherwise (pending / no record). Makes a
 * not-habilitado player visually clear (HU-62/HU-61).
 */
export const getHabilitacionColor = (
  isHabilitado?: boolean | null,
  status?: MedicalRecordStatus | null
): HabilitacionColor => {
  if (resolveIsHabilitado(isHabilitado, status)) {
    return 'success';
  }

  return status === MedicalRecordStatus.Rejected ? 'error' : 'warning';
};

/**
 * A short human explanation of the current medical-record situation, used as
 * the badge tooltip / secondary text.
 */
export const getMedicalRecordDetail = (
  status?: MedicalRecordStatus | null
): string => {
  if (!status) {
    return 'Sin ficha médica cargada';
  }

  return `Ficha médica: ${MEDICAL_RECORD_STATUS_LABELS[status]}`;
};
