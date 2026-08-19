# Sanction Appeal Workflow Specification

## Purpose

Characterizes the existing sanction appeal state machine (`PlayerSanctionStatus.SanctionAppealStatus`: `None`/`Pending`/`Accepted`/`Rejected`) currently implemented in `PlayerSanctionController`. This spec locks in current behavior via tests; it introduces no new behavior and no refactor.

## Requirements

### Requirement: Appeal Submission Guard

The system MUST reject a new appeal (`AppealPlayerSanction`) when the sanction's current `AppealStatus` is `Pending`, and MUST allow a new appeal from any other status (`None`, `Accepted`, `Rejected`), transitioning it to `Pending`.

#### Scenario: Appeal blocked while already pending

- GIVEN a `PlayerSanction` with `AppealStatus == Pending`
- WHEN `AppealPlayerSanction` is called for that sanction
- THEN the request is rejected (400)
- AND `AppealStatus` remains `Pending`

#### Scenario: Appeal succeeds from no prior appeal

- GIVEN a `PlayerSanction` with `AppealStatus == None`
- WHEN `AppealPlayerSanction` is called for that sanction
- THEN the request succeeds
- AND the persisted `AppealStatus` becomes `Pending`

#### Scenario: Appeal against a missing sanction

- GIVEN no `PlayerSanction` exists with the requested id
- WHEN `AppealPlayerSanction` is called with that id
- THEN the response is 404

### Requirement: Appeal Resolution Guard

The system MUST reject `ResolvePlayerSanctionAppeal` unless the sanction's current `AppealStatus` is `Pending`. When `Pending`, resolving with an "accept" decision MUST transition `AppealStatus` to `Accepted`; resolving with a "reject" decision MUST transition it to `Rejected`.

#### Scenario: Resolution blocked when not pending

- GIVEN a `PlayerSanction` with `AppealStatus == None` (or `Accepted`/`Rejected`)
- WHEN `ResolvePlayerSanctionAppeal` is called for that sanction
- THEN the request is rejected (400)
- AND `AppealStatus` is unchanged

#### Scenario: Resolution accepts a pending appeal

- GIVEN a `PlayerSanction` with `AppealStatus == Pending`
- WHEN `ResolvePlayerSanctionAppeal` is called with an "accept" decision
- THEN the persisted `AppealStatus` becomes `Accepted`

#### Scenario: Resolution rejects a pending appeal

- GIVEN a `PlayerSanction` with `AppealStatus == Pending`
- WHEN `ResolvePlayerSanctionAppeal` is called with a "reject" decision
- THEN the persisted `AppealStatus` becomes `Rejected`

#### Scenario: Resolution against a missing sanction

- GIVEN no `PlayerSanction` exists with the requested id
- WHEN `ResolvePlayerSanctionAppeal` is called with that id
- THEN the response is 404

## Non-Goals

- No extraction of this state machine into a dedicated service.
- No change to the guard clauses, transition rules, or HTTP status codes described above.
