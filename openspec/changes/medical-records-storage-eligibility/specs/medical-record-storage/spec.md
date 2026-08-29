# Medical Record Storage Specification

## Purpose

Defines where medical-record PDFs are stored, the storage boundary that isolates
them from public assets, the object-key scheme, and authenticated streaming as
the sole read path. New capability; no prior spec.

## Requirements

### Requirement: Private Medical-Records Bucket

Medical-record PDFs MUST be stored in the private Supabase bucket named by config
key `SupaBase:MedicalRecordsBucketName` (default `medical-records`). They MUST NOT
be written to the `public-images` bucket (`SupaBase:BucketName`).

#### Scenario: Upload lands in the private bucket

- GIVEN a valid PDF submitted to the medical-record upload endpoint
- WHEN storage persists the file
- THEN the object is created in the `medical-records` bucket
- AND nothing new is written to `public-images`

### Requirement: Object Key Scheme

The stored object key MUST be `{teamId}/{playerId}/{guid}{ext}` — no
`medical-records/` path prefix and no `tournamentId` segment. Uniqueness holds
because a team belongs to exactly one tournament.

#### Scenario: Key format

- GIVEN team `T`, player `P`, and an uploaded `ficha.pdf`
- WHEN the object key is built
- THEN it equals `T/P/{guid}.pdf` with a freshly generated guid

### Requirement: Bucket-Parameterized Raw Storage Boundary

`ISupabaseRawStorage` operations MUST accept a per-call target bucket that
defaults to `SupaBase:BucketName`. Callers that do not pass a bucket (image
uploads, `SupabaseBackupStorage`) MUST behave exactly as before this change.

#### Scenario: Default bucket preserved for existing callers

- GIVEN a team-logo upload or a database backup
- WHEN it calls the raw storage boundary without a bucket argument
- THEN the operation targets `public-images` as before

#### Scenario: Medical storage overrides the bucket

- GIVEN the medical-record storage adapter
- WHEN it calls the raw storage boundary
- THEN it passes the `medical-records` bucket explicitly

### Requirement: Authenticated Streaming Is the Only Read Path

A stored medical file MUST be readable only through the authenticated backend
download endpoint, which performs a service-role download and streams the bytes.
The system MUST NOT generate a public URL or a signed URL for a medical file. The
endpoint MUST require `AdminOrOwner` authorization.

#### Scenario: Owner downloads via streaming endpoint

- GIVEN an authenticated admin or the player's owner
- WHEN they call the medical-record download endpoint
- THEN the API responds with the PDF bytes streamed through the backend

#### Scenario: No public or signed URL is emitted

- GIVEN any medical-record read surface
- WHEN a response is built
- THEN no Supabase public or signed URL for the file is present

#### Scenario: Unauthorized caller rejected

- GIVEN a caller who is neither admin nor the player's owner
- WHEN they call the download endpoint
- THEN the response is 401 or 403, never the file

### Requirement: Upload Resets Status and Preserves Reupload Guard

Uploading a medical file MUST continue to force `MedicalRecordStatus` back to
`Pending`, and the existing 409-on-reupload-when-`Approved` guards (controller
pre-check and service check) MUST remain unchanged.

#### Scenario: Upload forces Pending

- GIVEN a registration with status `Rejected`
- WHEN a new file is uploaded
- THEN the status becomes `Pending`

#### Scenario: Reupload while Approved is blocked

- GIVEN a registration with status `Approved`
- WHEN a client attempts to upload a new file
- THEN the response is 409 with `ErrorMessages.MedicalRecord.AlreadyApproved`

## Non-Goals

- Making the `medical-records` bucket public or introducing signed/public URLs.
- Any production object-migration tool.
- Renaming the `MedicalRecordFileUrl` column.
- Stubbing Supabase in `CustomWebApplicationFactory`.
