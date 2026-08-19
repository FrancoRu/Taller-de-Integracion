# Test Infrastructure Specification

## Purpose

Stand up the minimal, working test-running capability on both Club12-Backend (.NET 8) and Club12-WebClient (React 18/Vite) so that later behavior-changing cleanup work can proceed under Strict TDD. This spec covers only the test harness itself — no business-logic behavior, no API contracts, and no coverage threshold are introduced or changed by this capability.

## Requirements

### Requirement: Backend Test Project

The solution MUST contain an xUnit test project that references the API project (or Application, whichever is required to host `WebApplicationFactory`-based integration tests) and is registered in `Club12-Backend/Solution/Club12.sln`.

#### Scenario: Test project is part of the solution

- GIVEN the solution file `Club12-Backend/Solution/Club12.sln`
- WHEN it is opened or built
- THEN it includes an xUnit test project
- AND that project references the API project needed for `WebApplicationFactory`

#### Scenario: dotnet test runs the smoke test successfully

- GIVEN the backend test project with `Microsoft.AspNetCore.Mvc.Testing` and coverlet configured
- WHEN `dotnet test` is run from the solution directory
- THEN the build succeeds
- AND at least one smoke test passes that issues an HTTP GET to a known API endpoint via `WebApplicationFactory` and asserts a 200 (or otherwise expected) response

### Requirement: Frontend Test Setup

Club12-WebClient MUST have Vitest and Testing Library configured and runnable via an npm script, without altering existing application code or Vite build behavior.

#### Scenario: Vitest is wired into the build config

- GIVEN `vite.config.ts` (or a dedicated `vitest.config.ts`) in `Club12-WebClient/`
- WHEN the Vitest test environment is resolved
- THEN it uses jsdom (or equivalent DOM environment) and Testing Library is available as a dependency

#### Scenario: npm test runs the smoke test successfully

- GIVEN `package.json` contains a `test` script that invokes Vitest
- WHEN `npm run test` (or `npm test`) is executed
- THEN it exits with a success status
- AND at least one smoke test passes that renders a simple existing component and asserts it appears in the DOM

### Requirement: Documented Test Commands

The exact commands to run backend and frontend tests MUST be documented in a README or CONTRIBUTING note so that later SDD `apply` phases can discover and run them without additional investigation.

#### Scenario: Commands are discoverable in repo docs

- GIVEN a README.md or CONTRIBUTING.md at the repo root or within each project folder
- WHEN a reader looks for how to run tests
- THEN the backend command (e.g. `dotnet test`) and frontend command (e.g. `npm run test`) are both listed with their working directories

### Requirement: No Behavior or Contract Changes

This capability MUST NOT modify any existing business logic, controller behavior, public API contracts, or component rendering output.

#### Scenario: Production code is unchanged

- GIVEN a diff of this change
- WHEN reviewed for modified files
- THEN no files outside test projects/config and documentation are modified
- AND no existing endpoint, DTO, or component behavior changes

## Non-Goals

- No specific coverage percentage is targeted or enforced by this change.
- No tests are written for existing business logic beyond the required smoke tests.
- No CI pipeline is added or modified in this change.
