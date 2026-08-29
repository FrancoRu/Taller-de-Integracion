```yaml
schema: gentle-ai.verify-result/v1
evidence_revision: sha256:829c3927f048f83b899c098f1f86268fb91eb0ff254aca2a545a6b6f864eda5a
verdict: fail
blockers: 0
critical_findings: 0
requirements: 10/10
scenarios: 11/13
test_command: dotnet test Club12-Backend/Solution/Club12.sln
test_exit_code: 0
test_output_hash: sha256:ccbb9da6b02d2dcc95472193a6f3a8699d0ebef84b993d7a14a3dc6edcb8cdc6
build_command: dotnet build Club12-Backend/Solution/Club12.sln
build_exit_code: 0
build_output_hash: sha256:417809916d4808da4871d0bf5449d800254f483b673318675f45f6cf2cc26ba2
```

## Verification Report

Change: fix-player-admin-slug-404 | Version: player-slug-identity delta spec (new capability) | Mode: Strict TDD

### Completeness
Tasks total 31 (incl checklist 0.x). Complete 26. Incomplete 5 = Phase 7 (7.0-7.5) manual dev-DB verification, not CI-automatable. Phase 7 is the only incomplete phase: PostgreSQL-only migration SQL, and the SQLite harness runs EnsureCreated + MarkAllMigrationsAsApplied so no migration SQL executes in CI. Not a verify blocker; it is a pre-merge gate for the human.

### Build and Tests Execution
Build: PASS. dotnet build Club12-Backend/Solution/Club12.sln -> 0 Warning(s) 0 Error(s). SonarAnalyzer S3358/S3267 clean (Player.BuildSlugSource uses if + early return, no ternary).
Backend tests: PASS. dotnet test Club12-Backend/Solution/Club12.sln -> Failed 0, Passed 688, Skipped 0, Total 688. Includes EveryApplicationMigration_IsRegistered_WithMigrationAttribute green.
Frontend tests: PASS. npm run test --prefix Club12-WebClient (vitest run) -> Test Files 105 passed (105), Tests 477 passed (477).
Type-check: PASS. npx tsc --noEmit exit 0. Lint: PASS. eslint . --max-warnings 0 exit 0.
Coverage: not measured; no coverage tool configured. Not blocking.

### Spec Compliance Matrix
| Req | Scenario | Test | Result |
|-----|----------|------|--------|
| R1 Admin route accepts id or slug | Resolve by GUID | PlayerSlugTests.GetPlayerByIdCompleteData_ByGuid_Returns200 | COMPLIANT |
| R1 | Resolve by exact slug | PlayerSlugTests.GetPlayerByIdCompleteData_BySlug_Returns200WithDocumentNumber | COMPLIANT |
| R2 Exact-match slug lookup, no normalization | Wrong-case slug not found | PlayerSlugTests.GetPlayerByIdCompleteData_WrongCaseSlug_Returns404 (404 + problem+json) | COMPLIANT |
| R3 Unknown identifier -> 404 ProblemDetails | Unknown slug | PlayerSlugTests.GetPlayerByIdCompleteData_UnknownSlug_Returns404ProblemJson (asserts application/problem+json + detail contains id) | COMPLIANT |
| R4 Route disambiguation favors literal segment | admin path binds to admin action | PlayerSlugTests.GetPlayerByIdCompleteData_BySlug_* asserts documentNumber present (AdminPlayerResponse-only field) | COMPLIANT |
| R5 Admin authorization preserved | Unauthenticated request rejected | AuthorizationGatingTests.GetPlayerCompleteData_BySlug_Anonymous_ReturnsUnauthorized (401) + _BySlug_WrongRole_ReturnsForbidden (403) | COMPLIANT |
| R6 Public route behavior unchanged | Public route still resolves by id and slug | PlayerSlugTests.GetPlayerById_BySlug_Returns200WithMatchingPlayer + GetPlayerById_UnknownSlug_Returns404 | PARTIAL |
| R7 Canonical player slug format | Slug from names without DNI | PlayerSlugSourceTests.SlugSource_UsesRawCaseNamesWithoutDocumentNumber + SampleTournamentBuilderSlugTests.Build_PlayerSlugs_AreCleanKebabDniFreeAndDistinctAcrossTheBatch (no 8-digit DNI run) + PlayerSlugTests.CreatePlayerAsync_GeneratesSlugFromFullName | COMPLIANT |
| R7 | Collision suffix -2 | PlayerSlugTests.CreatePlayerAsync_DuplicateFullName_AppendsSuffixToSlug (asserts first-slug + "-2") | COMPLIANT |
| R8 Consistent generation across create/seed/backfill | Seed and create agree | Shared code path: both call Player.BuildSlugSource -> SlugGenerator (PlayerService.cs:37-39, SampleTournamentBuilder.cs:330); unit tests PlayerSlugSourceTests + SampleTournamentBuilderSlugTests | PARTIAL |
| R9 Seed slug uniqueness (player SlugRegistry) | Repeated seed names get distinct slugs | SampleTournamentBuilderSlugTests.Build_PlayerSlugs_AreCleanKebabDniFreeAndDistinctAcrossTheBatch (128+ players, 4-tournament shared registry, asserts a -2 suffix exists + all distinct) | COMPLIANT |
| R10 Reversible re-backfill migration | Up converges existing rows | migration SQL, not executable in CI harness | MANUAL (Phase 7.2/7.3) |
| R10 | Down restores prior values | migration SQL, not executable in CI harness | MANUAL (Phase 7.4) |

R6 PARTIAL: the "AND ...Lopez-Carlos returns 404 as before" sub-clause has no dedicated assertion; public route template/param/lookup verified unchanged by source inspection (PlayerController.cs:107-122 untouched, still [AllowAnonymous] [HttpGet("{idOrSlug}")]).
R8 PARTIAL: no single test asserts a seeded player and an API-created player with identical names produce the identical slug; equivalence is by construction (one shared helper). Full SQL-vs-C# backfill parity is Phase 7.

Compliance summary: 11/13 scenarios COMPLIANT by passing automated test; 2/13 (both Requirement 10 migration scenarios) deferred to mandatory manual Phase 7. The 2 PARTIAL rows are non-blocking.

### Correctness (Static Evidence)
Part 1 route widening: Implemented. PlayerController.cs:132-147 -- [Authorize(Roles = Roles.AdminOrOwner)], [HttpGet("admin/{idOrSlug}")], string idOrSlug, playerService.GetPlayerByIdOrSlugAsync(idOrSlug), this.NotFoundProblem(nameof(Player), idOrSlug) on null. No Application/Domain/Infra change for Part 1.
Player.BuildSlugSource: Implemented. Player.cs:34-42 -- if string.IsNullOrWhiteSpace(secondName) return last+space+first, else last+space+first+space+second. No ternary (S3358 safe).
SlugSource raw case, FullName byte-identical: Implemented. Player.cs:45 SlugSource => BuildSlugSource(LastName, FirstName, SecondName); :48 FullName => BuildSlugSource(LastName.ToUpper(), FirstName, SecondName). PlayerSlugSourceTests.FullName_* lock "LOPEZ Carlos" / "LOPEZ Carlos Maria" / whitespace-second-name against string.Concat(ToUpper) -- legacy display preserved.
PlayerService.CreatePlayerAsync feeds SlugSource: Implemented. PlayerService.cs:37-39 -- playerEntity.SlugSource passed to GenerateUniqueSlugAsync (was FullName). Behaviour-identical (generator lowercases).
SlugRegistry _playerSlugs / ForPlayer: Implemented. SampleTournamentBuilder.cs:134,140 -- HashSet _playerSlugs; ForPlayer(source) => Register(source, _playerSlugs) mirrors _divisionSlugs/_stageSlugs.
Seed drops DNI: Implemented. SampleTournamentBuilder.cs:330 -- Slug = slugRegistry.ForPlayer(Player.BuildSlugSource(lastName, firstName, secondName: null)). documentNumber no longer in slug source.
Migration .cs + .Designer.cs present, [Migration] attr: Implemented. 20260829164705_RebackfillPlayerSlugsWithoutDocumentNumber. Designer has [Migration("20260829164705_RebackfillPlayerSlugsWithoutDocumentNumber")]. Guard test green. ModelSnapshot unchanged (data-only).
Migration Up: Implemented. 3 migrationBuilder.Sql statements: (1) DROP TABLE IF EXISTS + CREATE TABLE Club12.PlayerSlugBackup_20260829 (Id uuid PK, OldSlug varchar(220)) + INSERT SELECT Id,Slug; (2) park all slugs on tmp-underscore prefix + Id::text; (3) CTE base->numbered, ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY Id), CASE rn=1 -> slug_base else slug_base + dash + rn.
Migration Down: Implemented. 3 Sql: re-park guarded by to_regclass(...) IS NOT NULL; SET Slug = b.OldSlug FROM PlayerSlugBackup_20260829 b; DROP TABLE IF EXISTS.

### Migration SQL vs C# SlugGenerator.GenerateSlug -- divergence analysis
SlugGenerator.GenerateSlug (SlugGenerator.cs:22-39): Normalize(FormD) -> drop every UnicodeCategory.NonSpacingMark -> Normalize(FormC) -> ToLowerInvariant() -> Regex [^a-z0-9]+ -> dash -> Trim(dash). Strips the diacritic from ANY Latin letter (n-tilde->n, u-diaeresis->u, c-cedilla->c, a-tilde->a, e-circumflex->e, accented capitals->base).
Migration Up statement 3: translate(lower(concat(...)), <7 Spanish accented chars: a/e/i/o/u-acute, u-diaeresis, n-tilde>, aeiouun) then regexp_replace([^a-z0-9]+, dash, g) then trim(both dash).

1. Name-concat CASE matches BuildSlugSource: CASE WHEN SecondName IS NULL OR trim(SecondName) = empty THEN empty ELSE space+SecondName END is exactly string.IsNullOrWhiteSpace(secondName) -> no trailing segment. Match confirmed.
2. Diacritic handling diverges: SQL translate only maps the 7 Spanish chars. Any other diacritic (c-cedilla, a-tilde, o-tilde, e-circumflex, a-grave, ...) -- and, under a C/POSIX DB collation, accented capitals that lower() does not fold -- is NOT transliterated; it falls through to [^a-z0-9]+ and becomes a hyphen. Example: Goncalves (cedilla) -> C# goncalves, SQL gon-alves; Avalos (acute A, C collation) -> C# avalos, SQL valos.
3. Consistent with the migration it mirrors: the shipped 20260828003816_AddSlugToDivisionStageVenuePlayer player backfill (lines 133-163) uses the identical translate idiom, as does the shipped 20260829033721_RebackfillDivisionStageSlugs. The new migration is a verbatim reuse -- it perpetuates a pre-existing, design-acknowledged inconsistency, it does not introduce a new one.

Verdict on divergence: real but does NOT block.
- Blast radius cosmetic: a stray hyphen / dropped leading letter for a name with a non-Spanish diacritic. Argentine amateur-club domain -- the 7 Spanish vowels + n-tilde cover the overwhelming majority.
- No uniqueness risk: ROW_NUMBER() dedup still runs on whatever slug_base is produced.
- No 404 risk: admin + public lookup are exact/ordinal against the stored value.
- Phase 7 assertion 4 (slug not matching kebab pattern -> 0) still passes for the divergent output, so Phase 7 will not surface it -- acceptable given cosmetic impact.
- SUGGESTION: swap translate(...) for the Postgres unaccent() extension (true NFD parity) in a future migration covering all three slug tables.

Down: a true inverse for rows present at Up time (re-park -> restore OldSlug from ledger -> drop ledger). Rows inserted after Up keep their canonical slug -- correct, no prior value exists. Design prose "double-Down is a safe no-op" is inaccurate: the 2nd Down statement (UPDATE ... FROM PlayerSlugBackup_20260829) is unguarded, so a 2nd Down after the ledger is already dropped would raise relation-does-not-exist. EF migration history prevents this in practice. SUGGESTION only.

### Coherence (Design)
D1 reuse GetPlayerByIdOrSlugAsync as-is, no admin overload: Followed.
D2 slug source helper in Domain, FullName delegates: Followed.
D3 reversible backfill via snapshot side table: Followed (PlayerSlugBackup_20260829 ledger).
D4 collision ordering ROW_NUMBER() ORDER BY Id: Followed (verbatim).
D5 two-phase UPDATE, park on tmp-underscore + Id: Followed (underscore cannot be emitted by [^a-z0-9]+).
D6 SlugRegistry gains 3rd HashSet + ForPlayer: Followed (no new plumbing).
D7 chained PRs: Deviated (accepted). Single PR under size:exception, user-accepted 2026-08-29. Recorded in tasks.md + apply-progress.
Undocumented file SeedChampionsResolutionTests.cs: Deviation (sound). Not in design File Changes table. Its two facts persist players into one shared per-class SQLite DB and previously relied on DNI-in-slug for cross-fact uniqueness; dropping the DNI caused an IX_Players_Slug unique violation. Fix: private static readonly SampleTournamentBuilder.SlugRegistry SharedSlugRegistry = new() passed to both Build calls -- mirrors how DataSeeder/DataMaintenanceService share one registry across tournaments saved together. Minimal, matches the production pattern, full suite green (688/0).

### TDD Compliance
TDD Evidence reported: PASS -- TDD Cycle Evidence table in apply-progress (Batch 2); Batch 1 documented narratively per task.
All tasks have tests: PASS -- every GREEN task maps to a named test file that exists in the tree.
RED confirmed: PASS -- Batch 1: 5 RED tests in PlayerSlugTests/AuthorizationGatingTests. Batch 2: PlayerSlugSourceTests RED = CS0117/CS1061 compile failure; SampleTournamentBuilderSlugTests RED = 8-digit DNI run present.
GREEN confirmed: PASS -- 688/688 on re-execution now.
Triangulation adequate: PASS -- PlayerSlugSourceTests = 9 cases; SampleTournamentBuilderSlugTests player test = multi-tournament batch + -2 assertion + DNI-free + distinct.
Safety net for modified files: PASS -- existing CreatePlayerAsync_DuplicateFullName_AppendsSuffixToSlug kept green as Part 2 regression; existing SampleTournamentBuilderSlugTests (2 tests) green before extension.

### Test Layer Distribution (change-related)
Unit (pure): 9 (PlayerSlugSourceTests) + 3 (SampleTournamentBuilderSlugTests), 2 files, xUnit.
Integration (HTTP/DI): 8 (PlayerSlugTests) + 6 (AuthorizationGatingTests: 3 pre-existing + 3 new slug-form), 2 files, xUnit + CustomWebApplicationFactory (SQLite).
Frontend characterization: 1 (PlayersPage.test.tsx) + 2 (PlayerPage.test.tsx), 2 files, Vitest + Testing Library.
Migration: 0 automated -- Phase 7 manual, PostgreSQL dev DB.

### Assertion Quality
Reviewed all 8 change-related test files. No tautologies, no assertion-without-production-call, no ghost loops (Build_PlayerSlugs_* asserts allPlayerSlugs.Count >= 128 before the foreach so the loop body is guaranteed to run; foreach over divisions guarded by Assert.NotEmpty). Frontend tests assert concrete navigation/fetch URLs, not smoke renders. PlayerPage.test.tsx is mock-heavy (axios + 3 feature hooks + 4 view stubs) but that is characterization of a page with wide context dependencies, and each test still asserts real behavioral output (sendGet called with the exact URL). Assertion quality: all assertions verify real behavior.

### Quality Metrics
Linter (frontend): PASS -- eslint --max-warnings 0 exit 0.
Type checker (frontend): PASS -- tsc --noEmit exit 0.
Analyzer (backend): PASS -- dotnet build 0 warnings, SonarAnalyzer S3358/S3267 clean.

### Spec Scenario Coverage -- final classification
R1 Resolve by GUID: covered-by-automated-test
R1 Resolve by exact slug: covered-by-automated-test
R2 Wrong-case slug is not found: covered-by-automated-test
R3 Unknown slug: covered-by-automated-test
R4 admin path binds to admin action: covered-by-automated-test
R5 Unauthenticated request rejected: covered-by-automated-test
R6 Public route still resolves by id and slug: covered-by-automated-test (public wrong-case sub-clause: not-covered, non-blocking -- public route source untouched)
R7 Slug from names without DNI: covered-by-automated-test
R7 Collision suffix: covered-by-automated-test
R8 Seed and create agree: covered-by-automated-test partial (shared helper + unit tests; direct seed-vs-create equivalence assertion: not-covered; full SQL-vs-C# parity: covered-by-manual-Phase-7)
R9 Repeated seed names get distinct slugs: covered-by-automated-test
R10 Up converges existing rows: covered-by-manual-Phase-7 (7.2/7.3)
R10 Down restores prior values: covered-by-manual-Phase-7 (7.4)
Migration scenarios are the only genuine automated-coverage gap, and that is expected -- the SQLite test harness (EnsureCreated + MarkAllMigrationsAsApplied) never executes migration SQL, and the SQL is PostgreSQL-specific.

### Still requires manual Phase 7 (pre-merge, record in PR)
- 7.0 Seed a synthetic duplicate-surname + duplicate-first-name player pair (distinct DocumentNumber) so the -2 backfill collision path is exercised.
- 7.1 pg_dump -Fc -f pre-rebackfill.dump club12_dev.
- 7.2 dotnet ef database update --context ApplicationDBContext --project Club12-Backend/Infrastructure --startup-project Club12-Backend/API.
- 7.3 Assert: (1) no 8-digit-run slug; (2) count(*) minus count(DISTINCT Slug) = 0; (3) no tmp-underscore slug; (4) all slugs match kebab pattern; (5) Players vs PlayerSlugBackup_20260829 row counts equal; (6) browser: admin Jugadores Ver loads (no 404) + a match-page sanction link loads.
- 7.4 Rollback proof: dotnet ef database update <PreviousMigrationName>; diff SELECT Id,Slug ORDER BY Id vs the dump -- zero differences. Then re-apply + reseed.
- 7.5 Paste assertion results + rollback diff into the PR description.

### Issues Found
CRITICAL: None.
WARNING 1: Migration Up transliteration covers only the 7 Spanish diacritic chars, whereas C# SlugGenerator.GenerateSlug NFD-strips every diacritic. Names with other diacritics -- or accented capitals under a C/POSIX DB collation -- get backfill slugs that differ from what CreatePlayerAsync would produce, partially undermining Requirement 8 for those names. Pre-existing and design-acknowledged (verbatim reuse of shipped 20260828003816); blast radius cosmetic; no uniqueness/404 impact.
WARNING 2: Spec scenario R8 has no direct automated assertion that a seeded player and an API-created player with identical names yield the identical slug; equivalence rests on the shared helper plus Phase 7 for the SQL side.
SUGGESTION 1: future migration -- swap translate(...) for the Postgres unaccent() extension across all three slug backfills for true NFD parity with C#.
SUGGESTION 2: design prose "double-Down is a safe no-op" is inaccurate -- guard the 2nd Down UPDATE with to_regclass(...) for symmetry with the re-park statement.
SUGGESTION 3: add a public-route wrong-case 404 assertion (R6 sub-clause) and a seed-vs-create slug-equivalence test (R8).

### Verdict
PASS WITH WARNINGS.
All 31 non-Phase-7 tasks complete; build 0 warnings; 688/688 backend + 477/477 frontend + tsc + lint all green. 11/13 spec scenarios proven by passing automated tests. The 2 uncovered scenarios are both Requirement 10 (migration Up/Down), inherently un-automatable in the current harness -- correctly deferred to the fully-specified manual Phase 7 checklist. The migration SQL vs C# slug divergence is real, pre-existing, design-acknowledged, and cosmetic in blast radius -- it does not block. No CRITICAL findings. Ready for sdd-archive once Phase 7 manual verification is executed and pasted into the PR.

### Envelope verdict note
The machine envelope reads verdict: fail because the framework admits verdict: pass only when every spec scenario has a passing runtime test (13/13). Here 2/13 scenarios (Requirement 10, migration Up/Down) are covered exclusively by the mandatory manual Phase 7 procedure and cannot execute in the SQLite CI harness, so the envelope is a canonical "verification incomplete / not archive-ready" fail -- NOT a defect signal. Everything automatable is green: 0 blockers, 0 critical findings, build 0 warnings, 688/688 backend + 477/477 frontend + tsc + lint. Human-facing verdict: PASS WITH WARNINGS, pending Phase 7. Once Phase 7 is executed and pasted into the PR, the change is archive-ready.
