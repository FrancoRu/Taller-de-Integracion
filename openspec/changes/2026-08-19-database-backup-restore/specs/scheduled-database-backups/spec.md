# Delta for Scheduled Database Backups

## ADDED Requirements

### Requirement: Scheduled Runs Share the Catalog Write Path With Manual Runs

The scheduled `DatabaseBackupHostedService` MUST delegate to the same create-backup use case used
by the on-demand Admin endpoint, so every successful scheduled attempt writes exactly one
`BackupRecord` (`Forma de creación = Job`) and scheduled/manual runs cannot execute concurrently.

#### Scenario: Scheduled attempt writes a catalog record

- GIVEN the scheduled interval elapses and the backup feature is enabled
- WHEN the scheduled attempt completes successfully
- THEN a `BackupRecord` with `Forma de creación = Job` is created via the shared use case

#### Scenario: Scheduled and manual runs do not overlap

- GIVEN a scheduled backup attempt is currently executing
- WHEN an Admin triggers a manual "Generar respaldo" at the same time
- THEN the manual request does not run concurrently with the scheduled attempt

### Requirement: Default Storage Target Is a Persistent Server Volume

In deployed environments, the backup feature MUST default to `Backup:Enabled=true` and a storage
target backed by a persistent, mounted server volume, not the unmounted `Local` target and not
Supabase Storage.

#### Scenario: Deployed backup persists across redeploy

- GIVEN the backend runs in a deployed environment with the default configuration
- WHEN a scheduled backup completes and the container is later redeployed
- THEN the backup file remains present on the mounted server volume

## MODIFIED Requirements

### Requirement: Keep-Last-N Retention Pruning

Given a list of existing backup entries with timestamps, spanning both scheduled (`Job`) and
manual (`Manual`) creation methods, the retention policy MUST identify and select for removal all
entries beyond the most recent `RetentionCount` from the combined pool, retaining exactly the
newest `RetentionCount` entries (or all entries if fewer exist) regardless of which method created
them.
(Previously: retention only considered a single undifferentiated list of entries; the shared-pool
behavior across `Manual`/`Job` creation methods is now explicit.)

#### Scenario: Entry count within retention limit

- GIVEN the number of existing backup entries is less than or equal to `RetentionCount`
- WHEN the pruning logic runs
- THEN no entries are selected for removal

#### Scenario: Entry count exceeds retention limit

- GIVEN more backup entries exist than `RetentionCount`, mixing `Manual` and `Job` entries
- WHEN the pruning logic runs
- THEN the oldest `(count - RetentionCount)` entries are selected for removal regardless of
  creation method
- AND the newest `RetentionCount` entries are retained

#### Scenario: Entries with identical timestamps

- GIVEN two or more backup entries share the same timestamp at the retention boundary
- WHEN the pruning logic runs
- THEN selection is deterministic (e.g., stable tie-break by name/sequence) and does not vary
  between runs on the same input
