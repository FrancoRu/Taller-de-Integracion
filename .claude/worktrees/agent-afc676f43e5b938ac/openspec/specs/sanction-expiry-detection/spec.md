# Sanction Expiry Detection Specification

## Purpose

Characterizes `PlayerSanctionService.GetExpiredSanctionsAsync`, the date-math predicate that determines which sanctions have expired as of a cutoff date. Test-only characterization — no production code changes.

## Requirements

### Requirement: Inclusive Expiry Boundary

`GetExpiredSanctionsAsync(cutoffDate)` MUST return every `PlayerSanction` where `IssuedDate.AddDays(Duration) <= cutoffDate`, evaluated using the SQLite-translated semantics of the `CustomWebApplicationFactory` test harness (see Known Test-Harness Limitation below).

#### Scenario: Sanction not yet expired is excluded

- GIVEN a sanction with `IssuedDate.AddDays(Duration)` one day after `cutoffDate`
- WHEN `GetExpiredSanctionsAsync(cutoffDate)` is called
- THEN the sanction is NOT included in the result

#### Scenario: Sanction exactly at the boundary is included

- GIVEN a sanction with `IssuedDate.AddDays(Duration)` equal to `cutoffDate`
- WHEN `GetExpiredSanctionsAsync(cutoffDate)` is called
- THEN the sanction IS included in the result

#### Scenario: Sanction expired well before cutoff is included

- GIVEN a sanction with `IssuedDate.AddDays(Duration)` several days before `cutoffDate`
- WHEN `GetExpiredSanctionsAsync(cutoffDate)` is called
- THEN the sanction IS included in the result

#### Scenario: No sanctions match

- GIVEN only non-expired sanctions exist
- WHEN `GetExpiredSanctionsAsync(cutoffDate)` is called
- THEN an empty collection is returned

### Requirement: Player Navigation Included

Each returned `PlayerSanction` MUST have its `Player` navigation property populated.

#### Scenario: Player is eagerly loaded

- GIVEN an expired sanction linked to a seeded `Player`
- WHEN `GetExpiredSanctionsAsync(cutoffDate)` is called
- THEN the returned sanction's `Player` property is non-null and matches the seeded player

## Known Test-Harness Limitation

- SQLite (test provider used by `CustomWebApplicationFactory`) may translate `DateTime.AddDays` comparisons differently than Npgsql (production). Tests MUST assert against the value actually produced by the SQLite-backed harness; if SQLite fails to translate the expression as expected, this MUST be documented in the test file rather than silently assumed equivalent to production Npgsql behavior.
