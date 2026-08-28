/**
 * The medical-record / eligibility status of a player's season registration
 * (HU-57). Mirrors the backend `Domain.Enums.MedicalRecordStatus` and is
 * serialized as a string on the medical-record and roster response DTOs.
 *
 * The status is scoped per player + team + tournament, so being `Approved`
 * in one season never carries over to another (HU-59). A player is
 * "habilitado" only when the record is {@link Approved}.
 * @enum MedicalRecordStatus
 */
export enum MedicalRecordStatus {
  /**
   * No medical record uploaded yet, or uploaded but not reviewed. The player
   * is NOT habilitado.
   */
  Pending = 'Pending',

  /**
   * The owner/admin reviewed and approved the record (HU-58): the player is
   * habilitado for that team and tournament (HU-57).
   */
  Approved = 'Approved',

  /**
   * The owner/admin rejected the record (HU-58), usually with a reason. The
   * player is NOT habilitado.
   */
  Rejected = 'Rejected',
}
