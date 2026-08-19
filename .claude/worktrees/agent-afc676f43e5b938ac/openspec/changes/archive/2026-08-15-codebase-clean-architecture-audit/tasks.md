# Tasks: Test Infrastructure Scaffolding

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~150 (range 120-220) |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr (review budget 800 lines) |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Backend xUnit harness + smoke test | PR 1 (same PR) | `dotnet test Club12-Backend/Solution/Club12.sln` | Real `WebApplicationFactory<Program>` boot, SQLite in-memory | Delete `API.Tests/`, revert `Club12.sln` + `Program.cs` |
| 2 | Frontend Vitest harness + smoke test | PR 1 (same PR) | `npm run test` (from `Club12-WebClient/`) | Real jsdom render via Testing Library | Delete `src/test/`, revert `package.json` + `vite.config.ts` |

Both units are independent of each other and can be built in parallel; they are combined into one PR because combined size is well under the 800-line budget.

## Phase 1: Backend Harness Foundation

- [x] 1.1 Create `Club12-Backend/API.Tests/API.Tests.csproj` (net8.0, `Nullable=enable`, `IsPackable=false`; packages `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.Sqlite`, `coverlet.collector`; `ProjectReference` to `../API/API.csproj`). Satisfies spec "Backend Test Project".
- [x] 1.2 Register `API.Tests` in `Club12-Backend/Solution/Club12.sln` (`dotnet sln add`). Satisfies scenario "Test project is part of the solution".
- [x] 1.3 **[Isolated, reviewable alone]** Append `public partial class Program { }` to `Club12-Backend/API/Program.cs` — visibility-only shim required by `WebApplicationFactory<Program>`; no runtime/behavior change (design Decision: `Program` visibility). Commit separately for focused review.

## Phase 2: Backend Smoke Test (TDD)

- [x] 2.1 RED: Write `Club12-Backend/API.Tests/CustomWebApplicationFactory.cs` (swap Npgsql `DbContextOptions` for a shared open SQLite in-memory connection, `Database.EnsureCreated()`) and `Club12-Backend/API.Tests/SmokeTests.cs` asserting `GET api/divisions/` (existing `[AllowAnonymous]` action in `DivisionController`) returns 200. Run `dotnet test` — expect it to fail/not compile before wiring is complete.
- [x] 2.2 GREEN: Complete the factory's DbContext swap so `dotnet test` builds and the smoke test passes. Satisfies scenario "dotnet test runs the smoke test successfully".
- [x] 2.3 REFACTOR: Verify no duplicate/leaked DbContext registration remains in the test host; keep the factory minimal.

## Phase 3: Frontend Harness Foundation (parallel to Phase 1-2)

- [x] 3.1 Add devDependencies to `Club12-WebClient/package.json`: `vitest`, `@vitest/coverage-v8`, `jsdom`, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`; add scripts `"test": "vitest run"`, `"test:watch": "vitest"`.
- [x] 3.2 Add a `test` block to `Club12-WebClient/vite.config.ts` (switch import to `defineConfig` from `vitest/config`; `globals: true`, `environment: 'jsdom'`, `setupFiles: './src/test/setup.ts'`, `css: true`), reusing the existing `react-swc` plugin and `tsconfigPaths` alias. Satisfies scenario "Vitest is wired into the build config".
- [x] 3.3 Create `Club12-WebClient/src/test/setup.ts` with `import '@testing-library/jest-dom'`.

## Phase 4: Frontend Smoke Test (TDD)

- [x] 4.1 RED: Write `Club12-WebClient/src/test/smoke.test.tsx` rendering `LoadingIndicator` (`src/views/core/components/LoadingIndicator.tsx`) and asserting `screen.getByText('Cargando...')` is present. Run `npm run test` — expect failure before Phase 3 harness exists.
- [x] 4.2 GREEN: With Phase 3 complete, confirm `npm run test` passes the smoke test unmodified. Satisfies scenario "npm test runs the smoke test successfully".

## Phase 5: Documentation

- [x] 5.1 Add a "Testing" section to root `README.md` documenting `dotnet test` (run from `Club12-Backend/Solution/`) and `npm run test` (run from `Club12-WebClient/`), each with its working directory. Satisfies "Commands are discoverable in repo docs".

## Notes

- Bootstrapping exception (Strict TDD): 1.1-1.3 and 3.1-3.3 install/configure the harness itself — no prior failing test can exist since the framework isn't installed yet. The actual smoke assertions (2.1→2.2, 4.1→4.2) are written test-first once each harness can run.
- No task modifies business logic, controller behavior, API contracts, or component output (spec: "No Behavior or Contract Changes").
