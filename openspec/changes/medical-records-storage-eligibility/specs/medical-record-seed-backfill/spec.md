# Medical Record Seed Backfill Specification

## Purpose

Defines a gated, idempotent, failure-tolerant seed step that uploads a real
medical PDF to the private bucket for seeded approved registrations and records
the resulting file reference. New capability; no prior spec.

## Requirements

### Requirement: Configurable Seed PDF Path

A new config key `Seed:MedicalRecordPath` (mirroring `Seed:LogosPath`) MUST point
at the medical PDF to upload, with a default constant in `DataSeeder` referencing
`ficha-medica-club12.pdf`.

#### Scenario: Default path used when key unset

- GIVEN `Seed:MedicalRecordPath` is not configured
- WHEN the seeder resolves the path
- THEN it falls back to the `DataSeeder` default constant

### Requirement: Backfill Target Selection

The step MUST upload the PDF for every `PlayerTeamRegistration` with
`MedicalRecordStatus == Approved` whose `MedicalRecordFileUrl` is null or an
old-scheme reference (starts with `medical-records/`). After a successful upload
it MUST set `MedicalRecordFileUrl` and `MedicalRecordFileName` to the new object.

#### Scenario: Approved row without a new-scheme file is backfilled

- GIVEN an `Approved` registration with a null file reference
- WHEN the seed step runs with a valid PDF path
- THEN the PDF is uploaded via `IMedicalRecordStorage.StoreAsync`
- AND the registration's file reference and file name are set to the new object

#### Scenario: Non-approved rows are skipped

- GIVEN a registration with status `Pending` or `Rejected`
- WHEN the seed step runs
- THEN no file is uploaded for it

### Requirement: Idempotent

The step MUST skip any registration whose `MedicalRecordFileUrl` already resolves
under the new `{teamId}/` scheme, so a second run performs no uploads.

#### Scenario: Second run is a no-op

- GIVEN the seed step already backfilled all approved registrations
- WHEN the step runs again
- THEN no uploads occur and no file references change

### Requirement: Failure-Tolerant

Each upload MUST be wrapped so that a failure is logged as a warning and the step
continues with the next registration. An upload failure MUST NEVER fail the
overall seed.

#### Scenario: One upload fails

- GIVEN storage rejects the upload for one registration
- WHEN the seed step runs
- THEN a warning is logged
- AND the remaining registrations are still processed
- AND `SeedAsync` completes successfully

### Requirement: Whole-Step Skip Guard

When `Seed:MedicalRecordPath` is unset or the referenced file does not exist, the
step MUST log a warning and skip entirely without error.

#### Scenario: Missing PDF file

- GIVEN `Seed:MedicalRecordPath` points at a non-existent file
- WHEN the seeder runs
- THEN the step is skipped with a warning and the seed continues

### Requirement: Seed:MedicalRecords Bypass Flag

The step MUST run during a normal reset seed regardless of the flag.
Additionally, `Seed:MedicalRecords=true` MUST let the step run as a standalone
backfill against an already-seeded database, past the skip-if-teams-exist guard.

#### Scenario: Runs during a normal reset seed

- GIVEN a reset seed with `Seed:MedicalRecords` unset
- WHEN `SeedAsync` runs
- THEN the medical-record backfill step executes

#### Scenario: Standalone backfill on a seeded database

- GIVEN a database that already has teams and `Seed:Reset` is false
- WHEN the app starts with `Seed:MedicalRecords=true`
- THEN the backfill step runs even though the normal seed short-circuits

### Requirement: Sample Builder Stops Assigning Fake File References

`SampleTournamentBuilder` MUST NOT assign `SampleMedicalRecordFileUrl` or any
other fake file reference. Approved registrations it builds MUST have a null file
reference, so before the backfill runs they correctly read as not habilitado.

#### Scenario: Seeded approved row has no file before backfill

- GIVEN the sample tournament is built
- WHEN an approved registration is created
- THEN its `MedicalRecordFileUrl` is null

## Non-Goals

- Re-deriving the `p < 7` habilitado rule; selection keys off
  `MedicalRecordStatus == Approved`.
- Migrating production storage objects.
- `RosterCopyService` copying medical fields across seasons.
