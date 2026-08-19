# Scheduled Database Backups Specification

## Purpose

Define an opt-in, in-process `BackgroundService` that periodically dumps the PostgreSQL database and retains only the most recent N dumps. This spec scopes formal requirements to logic that is unit-testable in isolation from the real `pg_dump` binary and Supabase network calls; actual dump execution and upload are explicitly out of automated test scope (see Non-Goals).

## Requirements

### Requirement: Interval-Based Backup Trigger

The system MUST attempt a backup once per elapsed configured interval while the backup feature is enabled, using a `PeriodicTimer`-driven (or equivalent injectable) trigger abstraction so the wait/trigger logic can be tested without invoking `pg_dump`.

#### Scenario: Interval elapses while enabled

- GIVEN the backup service is enabled with a configured interval
- WHEN the configured interval elapses
- THEN the service invokes one backup attempt

#### Scenario: Interval not yet elapsed

- GIVEN the backup service is enabled and running
- WHEN less time than the configured interval has passed since the last attempt
- THEN no new backup attempt is triggered

### Requirement: Backup Enabled Gate

The system MUST NOT perform any backup scheduling or dump attempt when `Backup:Enabled` is `false`, and MUST resume normal scheduled behavior when re-enabled.

#### Scenario: Disabled configuration no-ops

- GIVEN `Backup:Enabled` is `false`
- WHEN the hosted service runs
- THEN no backup attempt, dump invocation, or storage call occurs

#### Scenario: Enabled configuration schedules attempts

- GIVEN `Backup:Enabled` is `true` with a valid interval
- WHEN the hosted service runs
- THEN backup attempts are scheduled per the interval trigger logic

### Requirement: Keep-Last-N Retention Pruning

Given a list of existing backup entries with timestamps, the retention policy MUST identify and select for removal all entries beyond the most recent `RetentionCount`, retaining exactly the newest `RetentionCount` entries (or all entries if fewer exist).

#### Scenario: Entry count within retention limit

- GIVEN the number of existing backup entries is less than or equal to `RetentionCount`
- WHEN the pruning logic runs
- THEN no entries are selected for removal

#### Scenario: Entry count exceeds retention limit

- GIVEN more backup entries exist than `RetentionCount`
- WHEN the pruning logic runs
- THEN the oldest `(count - RetentionCount)` entries are selected for removal
- AND the newest `RetentionCount` entries are retained

#### Scenario: Entries with identical timestamps

- GIVEN two or more backup entries share the same timestamp at the retention boundary
- WHEN the pruning logic runs
- THEN selection is deterministic (e.g., stable tie-break by name/sequence) and does not vary between runs on the same input

### Requirement: Backup Failure Isolation

The system MUST log a failed backup attempt (e.g., dump binary not found, non-zero exit code, storage upload failure) and MUST NOT allow that failure to crash the host process or prevent other hosted/background services from continuing to run.

#### Scenario: Dump execution fails

- GIVEN the process-execution abstraction reports a non-zero exit code or throws when invoked
- WHEN a scheduled backup attempt runs
- THEN the failure is logged
- AND the host process and other background services continue running unaffected
- AND no partial/failed dump is passed to retention pruning

#### Scenario: Dump binary unavailable

- GIVEN the configured `pg_dump` path does not resolve to an executable
- WHEN a scheduled backup attempt runs
- THEN the failure is logged with an actionable message
- AND the service remains alive to attempt again on the next interval

## Non-Goals (Manual Verification Required)

The following are explicitly NOT covered by automated unit tests and require staging/manual verification instead, per the proposal's own risk assessment:

- Actual `pg_dump` execution against a real PostgreSQL database (test harness uses SQLite).
- Actual upload to the Supabase storage bucket, including credentials/network behavior.
- End-to-end validation that a produced dump file is restorable.

These require a documented manual sign-off in staging before `Backup:Enabled=true` is used in any environment.
