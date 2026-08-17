# Backend Code Quality — Cleanup Batch 1 Specification

## Purpose

Behavior-preserving mechanical cleanup of `Club12-Backend`: extract magic
strings/numbers to named constants, remove dead code, eliminate CS1998
(`async` with no `await`) warnings, and normalize primary-constructor
parameter naming across controllers. No product capability is added or
changed — this spec defines the equivalence contract the cleanup MUST
satisfy.

## Requirements

### Requirement: Knockout Match Count Constants

The system MUST replace the literal numeric values (`4`, `2`) used to
determine knockout-stage match counts in `Application/Services/MatchService.cs`
with named constants (following the existing
`Application/Utils/Constants/{Stage/MaxTeams, Scorer/ScoreConstants}.cs`
convention). Match generation output MUST remain byte-for-byte equivalent to
pre-change behavior for every previously supported knockout configuration.

#### Scenario: Knockout bracket generation unchanged after magic-number extraction

- GIVEN a tournament configured for the knockout stage
- WHEN match generation runs for the team counts previously governed by the
  `4`/`2` literals
- THEN the number of generated matches, their stage assignment, and the
  number of scorer slots per match MUST be identical to the values produced
  before the constant extraction
- AND an automated test asserts these counts against the new named constants
  (not against a re-hardcoded literal), so a future typo'd constant value
  fails the test

### Requirement: Auth Scheme Constant

The system MUST extract the `"Bearer"` HTTP authentication scheme literal in
`API/Utils/StartupExtensions.cs:160-161` into a named constant without
changing the configured authentication scheme.

#### Scenario: JWT bearer authentication still configured after constant extraction

- GIVEN API startup configures JWT bearer authentication using the extracted
  constant
- WHEN the application builds and starts
- THEN the configured authentication scheme value MUST equal `"Bearer"`
- AND `dotnet build Club12-Backend/Solution/Club12.sln` completes with zero
  new errors or warnings

### Requirement: Dead Code Removal

The system MUST remove the unreachable commented-out block in
`API/Controllers/MatchController.cs:215-358`, the dead method in
`API/Controllers/TeamController.cs:190-251`, and the unused computed values
in `Application/Services/MatchService.cs:33-95`, without altering any
reachable code path, controller route, or response shape.

#### Scenario: Controllers and services behave identically after dead code removal

- GIVEN the identified dead code is deleted
- WHEN the existing backend test suite runs
  (`dotnet test Club12-Backend/Solution/Club12.sln`)
- THEN all previously passing tests MUST still pass
- AND `dotnet build` MUST produce zero new warnings or errors
- AND no controller route, HTTP verb, or public method signature changes

### Requirement: CS1998 Warning Elimination

The system MUST resolve the missing-`await` condition (CS1998) in the
`async` methods at `Application/Services/MatchService.cs:268-311` without
changing the method's return value, thrown exceptions, or execution order
relative to callers.

#### Scenario: Affected MatchService methods keep identical results

- GIVEN a method previously flagged with CS1998
- WHEN the method is invoked by existing callers after the fix
- THEN the returned value and any thrown exceptions MUST be identical to
  pre-change behavior
- AND `dotnet build` reports zero CS1998 warnings in the touched files

### Requirement: Controller Parameter Naming Normalization

The system MUST rename primary-constructor parameters in the 10 older
controllers from the underscore-prefixed convention to the no-underscore
convention already used by `UserController`/`AuthController`, touching only
declarations and internal usages (127 references).

#### Scenario: Controllers compile and respond identically after renaming

- GIVEN all `_`-prefixed primary-constructor parameter references are
  renamed to the no-underscore convention
- WHEN the solution is built and the existing test suite runs
- THEN `dotnet build` MUST succeed with zero new warnings or errors
- AND every existing test MUST still pass
- AND no public route, DTO shape, or HTTP contract changes

## Non-Goals

- No new business logic or functional behavior
- No controller route or contract changes
- No DTO shape changes
- No database schema changes
- Frontend (`Club12-WebClient`) is out of scope — deferred to
  `cleanup-mechanical-fixes-batch2`
