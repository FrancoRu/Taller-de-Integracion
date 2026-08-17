# Exploration: codebase-clean-architecture-audit

## Current State

**Backend layering** (`Club12-Backend/Solution/Club12.sln`) is structurally sound at the project-reference level: `Domain.csproj` has zero references; `Application.csproj` references only `Domain`; `Infrastructure.csproj` references `Application` + `Domain`; `API.csproj` references all three. No inverted/leaked references. DI composition lives in `API/Utils/StartupExtensions.cs` (reflection-based `RegisterScoped()` at line 296) and `API/Program.cs`.

All 10 controllers reviewed (`API/Controllers/*.cs`) are thin — no direct `DbContext`/EF usage — and go through `I*Service` + `IMapper`. Two coding eras coexist: an **older generation** (BlogPost, Division, Match, Player, Team, Tournament, Venue, PlayerStatistic) with `_`-prefixed primary-constructor params, inline `BadRequest($"...not found.")` strings, and commented-out dead code; a **newer generation** (User, Auth) with unprefixed params, `Domain.Enums.Roles` constants, `CancellationToken` threading, and a `User.GetCallerClaims()` extension.

Global exception handling (`API/Utils/GlobalExceptionHandler.cs`, `IExceptionHandler`, registered `Program.cs:28`) maps .NET exception types to `ProblemDetails` — a separate response-shaping path from controllers' inline `BadRequest(string)` pattern.

No test project exists anywhere in the backend solution or `Club12-WebClient/package.json` (no xUnit/NUnit/MSTest, no vitest/jest/playwright/cypress) — confirms known context; blocks safe TDD-driven remediation.

No secrets in tracked config: `API/appsettings.json` / `appsettings.Development.json` contain only Serilog config; connection strings come from `IConfiguration` at runtime with no literal committed. Per-dev appsettings are gitignored.

**Frontend** (`Club12-WebClient/src`) has a consistent `modules/{domain}/{context,hook,service,type}` + `views/{domain}` split across 13 domains, each repeating the same Context+Hook+Service triad. `tsconfig.json` has `strict: true`, `noUnusedLocals`, `noUnusedParameters`; grep for `: any`/`as any`/`@ts-ignore`/`@ts-expect-error` across `src/` returned **zero matches** — TS strictness is genuinely upheld. `languajes/spanish.ts` and `english.ts` exist but are **completely empty** — dead i18n scaffolding never wired up; all UI strings are hardcoded Spanish literals inline instead.

## Affected Areas

### Backend

- `API/Controllers/{BlogPost,Division,Match,Player,Team,Tournament,Venue,PlayerStatistic}Controller.cs` — 13+ occurrences return `BadRequest` (400) for "not found" instead of `NotFound` (404); e.g. `DivisionController.cs:67`, `MatchController.cs:79,101,123,204`, `PlayerController.cs:77,100,127`, `TeamController.cs:78,107,135,161`, `TournamentController.cs:67,94,161`, `VenueController.cs:67,94,123`, `BlogPostController.cs:75,103,128`, `PlayerStatisticController.cs:80,102`. A real cross-cutting HTTP-semantics bug.
- Same controller set — inconsistent primary-constructor param naming (`_`-prefixed vs. not); `UserController.cs:22-23`, `AuthController.cs:20` diverge from the rest.
- `API/Controllers/AuthController.cs:20,98-105` — `Logout` injects `UserManager<ApplicationUser>` (an `Infrastructure.Identity` type) directly into the API controller, bypassing the `IAuthenticationService`/`IUserManagementService` abstraction used elsewhere — a controller→Infrastructure leak.
- `API/Controllers/MatchController.cs:215-358` — ~140 lines of commented-out dead code; `TeamController.cs:190-251` — another ~60-line dead method.
- `Application/Services/MatchService.cs:33-95` — computes `playerStats`/`homeScorers`/`awayScorers` that are never used (half-finished feature).
- `Application/Services/MatchService.cs:268-311` — three `async` methods with no `await` (CS1998).
- `Application/Services/MatchService.cs:288-290` — magic numbers `4`/`2` for knockout match counts, despite an existing constants convention (`Application/Utils/Constants/Stage/MaxTeams.cs`, `.../Scorer/ScoreConstants.cs`) not applied here.
- `API/Utils/StartupExtensions.cs:160-161` — `"Bearer"` scheme name repeated as a magic string.
- `API/Utils/GlobalExceptionHandler.cs:74-86` vs. controllers — two disconnected error-response shapes for client errors.

### Frontend

- `modules/core/utils/axiosUtils.ts:270-285` (`sendGet`) bypasses the shared `sendRequest`/`throwError` pipeline (lines 183-232) — `triggerStatusCodeHandlers`/`handleUnauthorizedToken` (401→redirect) **never fire on GET requests**, a functional bug affecting most read traffic.
- `axiosUtils.ts:11` — `INVALID_TOKEN_PATH = '/token-invalido'` hardcoded, not sourced from `constants/routes.ts`.
- `modules/core/constants/routes.ts:2` — `apiUrl` hardcoded to `localhost`, no env-based prod/staging URL.
- `theme.ts:1` defines brand color, yet `#FD6B00`/`#d33` are re-hardcoded across **23 view files / 71 occurrences** (e.g. `TeamsPage.tsx:194,209,303,313,349`) instead of referencing the palette.
- `modules/core/languajes/{spanish,english}.ts` — empty files; dead i18n infra while views hardcode Spanish strings inline.
- `views/team/TeamsPage.tsx` (602 lines) — single component owns fetching, filter/pagination state, two dialog forms, delete confirmation, and rendering — no container/presentational split; this shape repeats across most `views/*Page.tsx` files.
- Query keys (e.g. `['blogPost','byId',id]`) are inline literals repeated ~13x per module instead of a shared factory.
- `modules/blogPost/service/blogPost.service.ts:31-33,53-57,73` — `FormData.append('Author'|'Title'|'MarkdownText'|'PhotoFile', ...)` magic strings must match backend DTO property names with no enforced contract.

### Cross-cutting

- Backend default `PageSize` = 100 (`Application/DTOs/Abstract/Request/PaginatedFilterRequest.cs:13`) vs. frontend `TABLE_ROWS_PER_PAGE` = 10 (`constants/pagination.ts:3`) — two independent magic numbers for the same concern.
- `ProblemDetails` (unhandled exceptions) vs. bare-string `BadRequest` (not-found) — two response shapes the frontend `useError`/`AxiosError` handling must reconcile inconsistently.
- README/docs-vs-structure accuracy not deeply audited this pass — flag as follow-up in `sdd-propose` if user-facing docs exist.

## Approaches

1. **Single mega-change fixing everything** — Pros: one coherent narrative. Cons: blows the 400/800-line PR budget by a wide margin (20+ backend, 25+ frontend files touched); zero test coverage means high regression risk. Effort: High.
2. **Phased remediation, chained/sliced PRs** — (a) stand up minimal test scaffolding (xUnit backend, Vitest frontend) first; (b) fix mechanical/high-confidence issues (400→404, `sendGet` pipeline bug, magic color/string extraction) as small independent slices; (c) structural issues (controller param naming, dead code, `TeamsPage`-style decomposition) per-domain slices. Pros: fits review-workload guard, unblocks TDD before risky refactors, isolated blast radius. Cons: longer overall, needs careful `sdd-tasks` sequencing. Effort: Medium per slice, High cumulative.
3. **Audit-only, no remediation in this change** — Pros: matches what was scoped, keeps this change reviewable. Cons: defers value. Effort: Low.

## Recommendation

Approach 3 for this change (explore-only, as scoped), feeding into Approach 2 for follow-on remediation via `sdd-propose`. Do NOT propose one big remediation PR — the review-workload guard (single-PR / 800-line budget) and the total absence of test coverage make that unsafe. `sdd-propose` should call out "add minimal test scaffolding" as a prerequisite slice before any behavior-changing fix (400→404 status change and the `sendGet` bug are both behavior changes, not style).

## Risks

- No test runner on either side — highest risk; any remediation is currently unverifiable except by manual QA.
- The `sendGet` 401-handler gap is a functional bug affecting session-expiry redirects on the majority of frontend traffic — needs explicit regression coverage before merge.
- 400→404 for "not found" is a breaking API-contract change for any frontend code branching on status 400 — needs a coordinated BE+FE slice.
- Large surface area (45+ files across findings) risks scope creep in `sdd-propose`/`sdd-tasks` without deliberate slicing.

## Ready for Proposal

Yes — proceed to `sdd-propose`, recommending a phased remediation program (test scaffolding first, then mechanical fixes, then structural refactors).
