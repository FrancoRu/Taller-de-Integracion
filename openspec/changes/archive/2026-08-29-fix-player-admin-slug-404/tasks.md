# Tasks: Admin Player Detail Accepts Slug + Unified Player Slug Format

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | Part 1 ~290 authored; Part 2 ~210 authored + ~2,000–2,500 generated `.Designer.cs` |
| 400-line budget risk | High |
| 800-line budget | Part 1 fits alone; combined diff ~2,700 rendered lines |
| Chained PRs recommended | Yes |
| Suggested split | PR #1 = Part 1 alone; PR #2 (targets PR #1 branch) = Part 2 |
| Delivery strategy | exception-ok (user accepted `size:exception` — single PR) |
| Chain strategy | n/a (single PR) |

Decision resolved before apply: single PR under `size:exception` (user-accepted 2026-08-29)
Chained PRs recommended: Yes (declined in favor of `size:exception`)
400-line budget risk: High — accepted as `size:exception`

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Part 1 — widen admin route to id-or-slug, fixes the 404 | PR #1 (base = tracker branch off `develop`) | `dotnet test Club12-Backend/Solution/Club12.sln --filter PlayerSlugTests` | Browser: admin → Jugadores → "Ver" loads | Revert `PlayerController.cs` commit; no schema/data state |
| 2 | Part 2 — unify slug format: helper + service + seed + reversible re-backfill migration | PR #2 (base = PR #1 branch) | `dotnet test Club12-Backend/Solution/Club12.sln --filter SampleTournamentBuilderSlugTests` | Manual dev-DB procedure (Phase 6) — not CI-automatable | `dotnet ef database update <previous>` restores slugs from ledger; then revert helper/seed/service commits |

## Pre-Apply Checklist (resolve before sdd-apply)

- [x] 0.1 Delivery decision: **single PR under `size:exception`** (user-accepted 2026-08-29). Parts 1 and 2 ship together; no PR chain.
- [x] 0.2 Dev DB is **seed-only** for now. Phase 7 MUST insert a synthetic duplicate-surname player pair before running assertions (else uniqueness assertion 2 is unexercised).
- [x] 0.3 Ledger table `PlayerSlugBackup_20260829` stays **permanent** for now — no follow-up drop migration. Revisit after release is confirmed.

---

## PART 1 — 404 Fix (PR #1)

### Phase 1: Backend RED (strict TDD) — `API.Tests`

- [x] 1.1 RED `PlayerSlugTests.cs`: `GET api/players/admin/{slug}` → 200, body `documentNumber` present (Admin route accepts id-or-slug; route-not-shadowed).
- [x] 1.2 RED `PlayerSlugTests.cs`: `GET api/players/admin/{guid}` → 200 (GUID form regression, `matchPage.tsx:513`).
- [x] 1.3 RED `PlayerSlugTests.cs`: `GET api/players/admin/no-such-player` → 404 `application/problem+json` (Unknown identifier → 404 ProblemDetails).
- [x] 1.4 RED `PlayerSlugTests.cs`: `GET api/players/admin/Lopez-Carlos` (wrong case) → 404 (Exact-match, no normalization).
- [x] 1.5 RED `AuthorizationGatingTests.cs`: anonymous → 401, Guest → 403 on the slug form (Admin authorization preserved).

### Phase 2: Backend GREEN — `API/Controllers/PlayerController.cs:131-146`

- [x] 2.1 Change `[HttpGet("admin/{id:guid}")]` → `[HttpGet("admin/{idOrSlug}")]`, param `Guid id` → `string idOrSlug`, call `playerService.GetPlayerByIdOrSlugAsync(idOrSlug)`, `NotFoundProblem(nameof(Player), idOrSlug)`; update XML doc. No Application/Domain/Infra change.
- [x] 2.2 Verify 1.1–1.5 green; `dotnet build Club12-Backend/Solution/Club12.sln` 0 warnings.

### Phase 3: Frontend characterization tests — no source change

- [x] 3.1 Create `Club12-WebClient/src/views/player/PlayersPage.test.tsx`: "Ver" navigates to `/panel/jugadores/{row.slug}` (Vitest + Testing Library).
- [x] 3.2 Create `Club12-WebClient/src/views/player/PlayerPage.test.tsx`: issues `GET /api/players/admin/{param}` with axios mocked.
- [x] 3.3 Run `npm run test --prefix Club12-WebClient`.

---

## PART 2 — Slug Format Unification (PR #2, base = PR #1 branch)

### Phase 4: Pre-refactor verification + Domain/Application (strict TDD)

- [x] 4.1 Confirm the current `Player.FullName` body actually uppercases `LastName`; record the exact current expression. If it does NOT, adjust the `BuildSlugSource` plan so `FullName` display output does not change.
- [x] 4.2 RED `Player` unit tests (pure xUnit): `BuildSlugSource` with and without `SecondName`; `FullName` still uppercases `LastName` (Canonical slug format).
- [x] 4.3 GREEN `Domain/Entities/Models/Player.cs`: add `static string BuildSlugSource(lastName, firstName, secondName)`, `SlugSource` property; `FullName` delegates with `LastName.ToUpper()`.
- [x] 4.4 GREEN `Application/Services/PlayerService.cs:37-39`: use `playerEntity.SlugSource` instead of `FullName` for `SlugGenerator.GenerateUniqueSlugAsync`.
- [x] 4.5 Regression: existing `CreatePlayerAsync_DuplicateFullName_AppendsSuffixToSlug` stays green (`-2` on duplicate names).

### Phase 5: Seed registry — `Infrastructure/Persistance/SampleTournamentBuilder.cs`

- [x] 5.1 RED `SampleTournamentBuilderSlugTests.cs`: seeded player slugs are kebab-only, DNI-free (no 8-digit run), no GUID, distinct across the 4-tournament batch (Seed slug uniqueness; consistent generation).
- [x] 5.2 GREEN: add `HashSet<string> _playerSlugs` + `ForPlayer(string source)` (mirror `_divisionSlugs`/`_stageSlugs`, ~:130-153).
- [x] 5.3 GREEN at ~:327: `Slug = slugRegistry.ForPlayer(Player.BuildSlugSource(lastName, firstName, secondName: null))`; drop the DNI segment.
- [x] 5.4 Verify 5.1 green; `dotnet build` 0 warnings.

### Phase 6: Reversible re-backfill migration

- [x] 6.1 Run `dotnet ef migrations add RebackfillPlayerSlugsWithoutDocumentNumber --context ApplicationDBContext --project Club12-Backend/Infrastructure --startup-project Club12-Backend/API` (produces `.cs` + `.Designer.cs`; commit the Designer file). → `20260829164705_RebackfillPlayerSlugsWithoutDocumentNumber.{cs,Designer.cs}`; ModelSnapshot unchanged (data-only).
- [x] 6.2 `Up()`: (1) DROP+CREATE `Club12.PlayerSlugBackup_20260829` ledger and `INSERT SELECT "Id","Slug"` snapshot; (2) park all slugs on `'__tmp_' || "Id"::text`; (3) assign canonical slugs via the `20260828003816` CTE (`translate`/`regexp_replace`, `ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY "Id")`).
- [x] 6.3 `Down()`: re-park on `'__tmp_' || "Id"` guarded by `to_regclass`, restore `"Slug" = b."OldSlug"` from ledger, `DROP TABLE IF EXISTS` ledger.
- [x] 6.4 Verify guard test `EveryApplicationMigration_IsRegistered_WithMigrationAttribute` stays green; `dotnet test Club12-Backend/Solution/Club12.sln`. → 688 passed / 0 failed.

### Phase 7: Manual dev-DB verification — DONE (2026-08-29, Supabase dev DB `vjljacxfhoybvvnpbqog`, schema `Club12`)

- [x] 7.0 Dev DB is seed-only. Collision path covered on real data instead: post-migration `320` players, `304` slugs carry a `-N` suffix with `count(*) - count(DISTINCT "Slug") = 0` — the migration's `ROW_NUMBER()` dedup ran extensively without a uniqueness violation. (The synthetic-pair route was skipped as redundant.)
- [x] 7.2 `dotnet ef database update 20260829164705_RebackfillPlayerSlugsWithoutDocumentNumber` against the Supabase session pooler (port 5432). Applied clean.
- [x] 7.3 Assertions after apply: `dni_leak=0`, `dup_slugs=0`, `tmp_left=0`, `bad_shape=0`, `ledger_gap=0`, `ledger_exists=true`, `collision_suffixes=304`. Browser: admin → Jugadores → "Ver" loads (no 404); match-page sanction link to the player loads.
- [x] 7.4 Rollback proof: copied the ledger to `Club12._phase7_ledger_keep`, ran `dotnet ef database update 20260829101157_AddTeamPointDeduction`. Result: `mismatches=0` (every one of the 320 slugs restored to its `OldSlug`), `ledger_after_down = null` (Down dropped the ledger). Then re-applied forward (Paso C) so the DB sits on the new format; `_phase7_ledger_keep` dropped.
- [x] 7.5 Evidence recorded here and in the PR description.

### Phase 8: Full regression

- [x] 8.1 `dotnet test Club12-Backend/Solution/Club12.sln` green (688 passed / 0 failed); `dotnet build` 0 warnings (SonarAnalyzer S3358/S3267 clean).
- [x] 8.2 `npm run test --prefix Club12-WebClient` green (105 files / 477 tests); `npx tsc --noEmit` exit 0; `npm run lint` exit 0.
