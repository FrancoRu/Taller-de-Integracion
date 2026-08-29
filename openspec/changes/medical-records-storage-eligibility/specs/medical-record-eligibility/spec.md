# Medical Record Eligibility Specification

## Purpose

Defines "habilitado" (eligible to play) as requiring both an `Approved` medical
record and a real stored file, enforced on every read surface and at approve
time. New capability; no prior spec.

## Requirements

### Requirement: File-Backed Habilitación Rule

A player registration MUST be considered "habilitado" only when
`MedicalRecordStatus == Approved` AND `MedicalRecordFileUrl` is a non-null,
non-whitespace stored file reference. `Approved` without such a reference MUST
read as NOT habilitado.

#### Scenario: Approved with a stored file

- GIVEN a registration with status `Approved` and a non-whitespace file reference
- WHEN eligibility is computed
- THEN habilitado is true

#### Scenario: Approved without a stored file

- GIVEN a registration with status `Approved` and a null or whitespace file reference
- WHEN eligibility is computed
- THEN habilitado is false

### Requirement: Rule Applies at Every Read Surface

The file-backed rule MUST be applied consistently at: the transient
`Player.IsHabilitado`, `MedicalRecordResponse.IsHabilitado`, the public player
DTO (`PublicPlayerResponse.IsHabilitado`), the season roster load
(`TeamService`), and the match-sheet eligibility gate (`PlayerStatisticService`).

#### Scenario: Public player DTO reflects the rule

- GIVEN an `Approved`-without-file registration
- WHEN the public player endpoint returns the player
- THEN `isHabilitado` is false

#### Scenario: Season roster load reflects the rule

- GIVEN an `Approved`-without-file registration on a season roster
- WHEN the roster is loaded
- THEN that player reads as not habilitado

### Requirement: Match-Sheet Gate Rejects Approved-Without-File

The match-sheet eligibility gate MUST raise `PlayerNotEligible` for a player
whose registration is `Approved` but has no stored file, exactly as it does for a
non-`Approved` registration.

#### Scenario: Approved-without-file cannot be added to a match sheet

- GIVEN a player with an `Approved`-without-file registration
- WHEN they are added to a match sheet
- THEN `PlayerNotEligible` is raised

### Requirement: Approve-Time Write Guard

`MedicalRecordService.ReviewAsync` MUST reject a transition to `Approved` when
`MedicalRecordFileUrl` is null or whitespace, returning a Spanish message under
`ErrorMessages.MedicalRecord.*`. Transitions to `Pending` or `Rejected` MUST be
unchanged.

#### Scenario: Approve without a file is rejected

- GIVEN a registration with no stored file
- WHEN an admin approves it
- THEN the operation is rejected with a Spanish `ErrorMessages.MedicalRecord.*` message
- AND the stored status is unchanged

#### Scenario: Reject without a file is still allowed

- GIVEN a registration with no stored file
- WHEN an admin rejects it
- THEN the transition to `Rejected` succeeds

### Requirement: Effective Immediately, No Data Migration

On deploy, a previously-`Approved` registration whose `MedicalRecordFileUrl` is
null or whitespace MUST immediately read as NOT habilitado. This rule MUST NOT
run any data migration or backfill.

#### Scenario: Legacy approved row with no file after deploy

- GIVEN a registration approved before this change whose file reference is null
- WHEN eligibility is computed after deploy
- THEN habilitado is false
- AND no migration modified the row

### Requirement: Frontend Approve Action Disabled Without a File

The "Aprobar" action in the medical-record dialog MUST be disabled when there is
no stored file for the registration. This UX ships in this change.

#### Scenario: Aprobar disabled with no file

- GIVEN the medical-record dialog for a registration with no stored file
- WHEN it renders
- THEN the "Aprobar" button is disabled

#### Scenario: Aprobar enabled with a file

- GIVEN a registration that has a stored file
- WHEN the dialog renders
- THEN the "Aprobar" button is enabled

## Non-Goals

- A DB `CHECK` constraint or EF migration enforcing "Approved ⇒ file".
- Any change to the Pending / Rejected review flow or the 409 reupload guards.
- Backfilling existing rows as part of this rule (handled by
  `medical-record-seed-backfill`).
