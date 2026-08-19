# Design: Test Scaffolding for Clean-Architecture Audit

## Technical Approach

Stand up the two idiomatic, minimal test harnesses named in the proposal, touching only additive dev/test artifacts. Backend: a new xUnit integration project driven by `WebApplicationFactory<Program>`, booting the real `API` host with its DbContext swapped to SQLite in-memory. Frontend: Vitest + Testing Library on jsdom, configured inside the existing `vite.config.ts`. Each side ships exactly one green smoke test. No production code path is exercised or altered — the only production-file touch is a compiler-level visibility shim, isolated as a documented decision below.

## Architecture Decisions

### Decision: Backend test project name and layout
**Choice**: `Club12-Backend/API.Tests/API.Tests.csproj`, SDK `Microsoft.NET.Sdk`, `net8.0`, `Nullable=enable`, `IsPackable=false`; single `ProjectReference` to `..\API\API.csproj` (transitively pulls Application/Domain/Infrastructure).
**Alternatives considered**: bare `Tests` (matches flat sibling names but hides that it targets the API surface); per-layer `Application.Tests`+`Domain.Tests` now.
**Rationale**: `<Sut>.Tests` is the universally recognized .NET convention; folder==csproj matches the repo's existing style; one project keeps this change minimal. Per-layer unit projects are deferred to later behavior slices.

### Decision: Test database strategy
**Choice**: `CustomWebApplicationFactory` removes the registered Npgsql `DbContextOptions` descriptor and re-registers the DbContext on a shared open SQLite in-memory connection, then `Database.EnsureCreated()`.
**Alternatives considered**: EF Core InMemory provider; mock the DbContext; hit real Postgres.
**Rationale**: SQLite in-memory keeps relational semantics (constraints, transactions) so the harness is reusable by the later 400→404 and refactor slices; EF InMemory diverges from relational behavior and would mislead those tests; real Postgres is non-hermetic; mocking is brittle and skips the real query pipeline.

### Decision: `Program` visibility for WebApplicationFactory
**Choice**: append `public partial class Program { }` to `API/Program.cs`.
**Alternatives considered**: assembly marker interface; skip WebApplicationFactory and ship a trivial assert-only smoke test.
**Rationale**: this is the standard `Mvc.Testing` requirement and is strictly a visibility shim — it changes no runtime behavior or code path (see Open Questions re: the proposal's "zero files modified" criterion). Any alternative that avoids it either needs another public production type or abandons the harness the proposal requires.

### Decision: Frontend config location
**Choice**: add a `test` block to the existing `vite.config.ts`, switching its import to `defineConfig` from `vitest/config`; `globals: true`, `environment: 'jsdom'`, `setupFiles: './src/test/setup.ts'`, `css: true`.
**Alternatives considered**: separate `vitest.config.ts`.
**Rationale**: single source of truth reuses the existing `react-swc` plugin and the `@/*` alias via `vite-tsconfig-paths`; a separate file would re-declare plugins/aliases and risk drift. `vitest/config` merges Vite+Vitest types so the `test` block type-checks.

## Data Flow

    dotnet test ─→ API.Tests ─→ CustomWebApplicationFactory ─→ API host (SQLite in-mem) ─→ HTTP smoke assert
    npm run test ─→ Vitest ─→ jsdom + RTL ─→ render smoke component ─→ jest-dom assert

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Club12-Backend/API.Tests/API.Tests.csproj` | Create | xUnit project, packages, ref to API |
| `Club12-Backend/API.Tests/CustomWebApplicationFactory.cs` | Create | Swaps DbContext to SQLite in-memory |
| `Club12-Backend/API.Tests/SmokeTests.cs` | Create | Boots host, asserts anonymous endpoint non-5xx |
| `Club12-Backend/Solution/Club12.sln` | Modify | Add project + GUID/config rows (`dotnet sln add`) |
| `Club12-Backend/API/Program.cs` | Modify | Append `public partial class Program { }` visibility shim |
| `Club12-WebClient/package.json` | Modify | Add dev deps + `test`/`test:watch` scripts |
| `Club12-WebClient/vite.config.ts` | Modify | Add `test` block via `vitest/config` |
| `Club12-WebClient/src/test/setup.ts` | Create | `import '@testing-library/jest-dom'` |
| `Club12-WebClient/src/test/smoke.test.tsx` | Create | Renders trivial component, jest-dom assert |

## Interfaces / Contracts

Backend packages (8.0.25 line where applicable): `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.Sqlite`, `coverlet.collector`.
Frontend dev deps: `vitest` ^3.2 (Vite 7 support), `@vitest/coverage-v8`, `jsdom`, `@testing-library/react` ^16, `@testing-library/jest-dom` ^6, `@testing-library/user-event` ^14. Scripts: `"test": "vitest run"`, `"test:watch": "vitest"`.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit (FE) | Harness boots, matcher works | Render trivial component, assert with jest-dom |
| Integration (BE) | Host builds, pipeline responds | WebApplicationFactory + SQLite, anonymous request non-5xx |
| E2E | N/A this change | Deferred |

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary. Purely additive dev/test dependencies and config.

## Migration / Rollout

No migration. Rollback = delete `API.Tests`, revert the four modified files.

## Open Questions

- [ ] Proposal success criterion says "zero production files modified," but idiomatic `WebApplicationFactory` needs the one-line `Program` visibility shim. Recommend accepting it as behavior-neutral; confirm at tasks/apply.
