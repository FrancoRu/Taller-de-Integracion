# Database Restore Specification

## Purpose

Define Admin-only restore of a catalogued backup directly over the live database, behind an
explicit maintenance-mode window, with mandatory confirmation, an automatic pre-restore safety
backup, and failure isolation. The parallel-DB-plus-swap alternative is explicitly rejected.

## Requirements

### Requirement: Restore Is Admin-Only

The system MUST reject restore requests from any caller without the `Admin` role.

#### Scenario: Non-Admin caller rejected

- GIVEN an authenticated caller without the `Admin` role
- WHEN they call the restore endpoint
- THEN the request is rejected with an authorization error
- AND no restore is attempted

### Requirement: Restore Requires Explicit Confirmation

The system MUST require an explicit confirmation step naming the target backup's `Fecha` before
executing a restore; an unconfirmed request MUST NOT proceed.

#### Scenario: Unconfirmed restore does not execute

- GIVEN an Admin initiates a restore without confirming
- WHEN the request is evaluated
- THEN the restore does not run

### Requirement: Automatic Pre-Restore Safety Backup

Every restore MUST take an automatic backup of the current database state (catalogued with
`Forma de creación = Job`) immediately before restoring, even if this exceeds `RetentionCount`.

#### Scenario: Safety backup precedes restore

- GIVEN an Admin confirms a restore of a selected backup
- WHEN the restore proceeds
- THEN a new backup of the current state is created and catalogued first
- AND only after it succeeds does the restore of the selected backup begin

### Requirement: Maintenance-Mode Window Wraps Restore

The system MUST enter maintenance mode before restore begins and exit it after restore completes
(success or failure). While in maintenance mode, non-`/health` endpoints MUST respond `503`. The
system MUST provide a manual escape hatch to force-exit a stuck maintenance window.

#### Scenario: Traffic rejected during restore

- GIVEN a restore is in progress
- WHEN a request is made to any endpoint other than `/health`
- THEN the response is `503`

#### Scenario: Maintenance mode clears after restore

- GIVEN a restore has completed, successfully or not
- WHEN the process is inspected
- THEN maintenance mode is no longer active and non-`/health` endpoints respond normally

#### Scenario: Manual escape hatch clears a stuck window

- GIVEN maintenance mode remains active without an in-progress restore (stuck window)
- WHEN an Admin invokes the manual exit action
- THEN maintenance mode is cleared
- AND the app resumes serving non-`/health` traffic

### Requirement: Restore Executes Directly Against the Live Database

The system MUST restore the selected catalogued backup directly over the live database (e.g. via
`pg_restore` or equivalent) rather than provisioning a parallel database and swapping.

#### Scenario: Selected backup becomes the live state

- GIVEN a catalogued backup and a confirmed restore request
- WHEN the restore completes successfully
- THEN the live database reflects the content of the selected backup

### Requirement: Restore Failure Is Logged and Isolated

A failed restore MUST be logged with an actionable message, MUST NOT crash the host process, and
MUST still clear the maintenance window so the app returns to a known state. Concurrent
backup/restore operations MUST be blocked by a single-flight guard.

#### Scenario: Restore failure is logged and app recovers

- GIVEN a restore attempt fails (e.g. `pg_restore` non-zero exit)
- WHEN the failure occurs
- THEN it is logged with an actionable message
- AND maintenance mode is cleared
- AND the host process continues running

#### Scenario: Concurrent backup blocked during restore

- GIVEN a restore is in progress
- WHEN a scheduled or manual backup attempt is triggered
- THEN it is rejected or queued, not run concurrently with the restore
