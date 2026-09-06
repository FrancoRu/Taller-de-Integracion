# Apply Progress — division-team-roster

## Batch 1 (this run): Phase 1 + Phase 2 — tasks 1.1–2.9

Status: **COMPLETE**. All tasks 1.1 through 2.9 implemented, TDD-verified, zero regressions.

### Phase 1 — Backend Domain & Infrastructure Foundation (1.1–1.8)

No TDD tests of their own (prerequisite scaffolding for Phase 2's tests to compile against), built first:

- 1.1 `Club12-Backend/Domain/Entities/Models/DivisionTeamRegistration.cs` — new entity, mirrors `TeamTournamentRegistration.cs` exactly (required `TeamId`/`DivisionId`, nullable navs, `EntityBase`, no status/lifecycle field, 3-line plain-prose summary).
- 1.2 `Club12-Backend/Domain/Entities/Models/Team.cs` — added `DivisionTeamRegistrations` nav collection.
- 1.3 `Club12-Backend/Domain/Entities/Models/Division.cs` — added `DivisionTeamRegistrations` nav collection.
- 1.4 `Club12-Backend/Infrastructure/Persistance/Configurations/DivisionTeamRegistrationEntityConfiguration.cs` — new, mirrors `TeamTournamentRegistrationEntityConfiguration.cs`; unique index `(TeamId, DivisionId)`, single-column index on `DivisionId`, both FKs `Cascade`. Auto-discovered via `ApplyConfigurationsFromAssembly` — no explicit registration needed (confirmed).
- 1.5 `Club12-Backend/Infrastructure/Persistance/EntityConstants.cs` — added `DivisionTeamRegistration = "DivisionTeamRegistrations"` alphabetically between `DivisionPlayoffMapping` and `Match`.
- 1.6 `Club12-Backend/Infrastructure/Persistance/ApplicationDBContext.cs` — added `DbSet<DivisionTeamRegistration> DivisionTeamRegistrations`.
  - **Unplanned but required fix**: `Club12-Backend/Infrastructure/Persistance/ApplicationDBContextFactory.cs` (the EF design-time factory) constructs `ApplicationDBContext` via an object initializer that must set every `required` DbSet — build failed with CS9035 until `DivisionTeamRegistrations = null!` was added there too. Not in tasks.md; a necessary consequence of the new `required` DbSet member.
- 1.7 `Club12-Backend/Domain/Entities/Models/Stage.cs` — added `DateTime? DrawnAt`. `Club12-Backend/Application/DTOs/Stage/Response/StageResponse.cs` — added matching `DrawnAt`. No `StageProfile.cs` change (confirmed convention-based AutoMapper map, per design §0/§8).
- 1.8 `Club12-Backend/Domain/Enums/AuditAction.cs` — added `PlayoffDraw` member.

Verified: `dotnet build Club12-Backend/Solution/Club12.sln` — 0 warnings, 0 errors after Phase 1.

### Phase 2 — Migration + Idempotent Backfill (2.1–2.9), strict TDD

**Test-harness constraint discovered and resolved (read this before touching Phase 2 again):** `CustomWebApplicationFactory` builds the SQLite test database via `EnsureCreated()` from the current EF model, not by replaying migrations — so a migration's raw PostgreSQL SQL (schema-qualified identifiers, `gen_random_uuid()`) cannot execute in the test suite. This exact situation already has a precedent in this codebase: `API.Tests/TeamTournamentRegistrationTests.cs`'s `Backfill_JoinLogic_RecoversHistoricalAndCurrentParticipationIdempotently` test replicates a migration's raw-SQL backfill as an equivalent EF/LINQ query run against real seeded rows in the SQLite test DB, rather than executing the SQL itself. `DivisionTeamRegistrationTests.cs` follows the identical pattern. This was a design interpretation required by the environment, not a deviation from tasks.md's intent — the task instructions on this apply job explicitly acknowledged the migration "doesn't need to run cleanly against SQLite specifically."

- 2.1–2.4 RED — `Club12-Backend/API.Tests/DivisionTeamRegistrationTests.cs` created with 4 tests:
  - `Backfill_TeamInTwoSubGroupsOfOneDivision_CollapsesToOneRegistration`
  - `Backfill_TeamInGroupAndSameDivisionBracket_CollapsesToOneRegistration`
  - `Backfill_CrossDivisionCupTeam_ProducesTwoRegistrations_NotCollapsed`
  - `Backfill_ReRunAgainstAlreadyBackfilledData_CreatesNoDuplicates`
  - Each seeds real `Tournament`/`Division`/`Stage`/`Team`/`StageTeamMatch` rows via the SQLite-backed `ApplicationDBContext`, then calls a private `RunBackfillAsync` helper that replicates the migration's `INSERT ... SELECT ... GROUP BY ... WHERE NOT EXISTS` logic as EF/LINQ, and asserts on the persisted `DivisionTeamRegistrations` rows.
  - Genuine RED verified: first wrote `RunBackfillAsync` with a deliberately wrong dedup key (`GroupBy(TeamId)` only, dropping `DivisionId`). Ran `dotnet test --filter DivisionTeamRegistrationTests` → 3 passed, 1 failed (`Backfill_CrossDivisionCupTeam_ProducesTwoRegistrations_NotCollapsed`, expected 2 got 1) — confirmed failing for the right reason (the exact bug the spec's dedup rule exists to prevent).
- 2.5–2.7 GREEN — ran the real EF CLI: `dotnet ef migrations add AddDivisionTeamRegistrationAndStageDrawnAt --project Infrastructure/Infrastructure.csproj --startup-project API/API.csproj --context ApplicationDBContext --output-dir Migrations` from `Club12-Backend/` (with `ConnectionStrings__DbConnection` env var set so the design-time factory could build; the API host's own DI path failed on missing JWT config and EF fell back to `ApplicationDBContextFactory`, which is expected in this repo — see that factory's own doc comment). Migration generated at `Club12-Backend/Infrastructure/Migrations/20260906005632_AddDivisionTeamRegistrationAndStageDrawnAt.cs` (+ `.Designer.cs`), auto-diffed the exact expected schema (table, 3 indexes matching design's naming, 2 cascade FKs, nullable `Stages.DrawnAt` column). Hand-edited `Up()` to append the raw backfill SQL from design §1.5(b) after the `CreateIndex` calls (`gen_random_uuid()` — already proven safe in this codebase, see `AddPlayerTeamRegistrationTable.cs`'s existing use, so no `uuid-ossp` fallback needed). Reordered `Down()` to `DropColumn("DrawnAt")` then `DropTable("DivisionTeamRegistrations")`, per design §6's explicit ordering (EF's auto-generated order was table-then-column; functionally equivalent either way, but design calls for this exact order).
  - `ApplicationDBContextModelSnapshot.cs` regenerated automatically by the `migrations add` command; diff reviewed and is exactly the expected additive schema (no unrelated changes).
- 2.8 GREEN verified — `dotnet test API.Tests/API.Tests.csproj --filter DivisionTeamRegistrationTests` → 4/4 passing after fixing the dedup key back to the correct `(TeamId, DivisionId)` pair via `.Distinct()` on the joined tuple.
- 2.9 — `Club12-Backend/Infrastructure/Persistance/Seeding/SampleTournamentBuilder.cs`: added a `DivisionTeamRegistration` per team for its regular division (mirrors the `TeamTournamentRegistration` seed step), and a second one per team for the cross-division-cup division when `SeedCrossDivisionCup` runs (so seeded cross-cup tournaments satisfy the roster invariant of one regular + one cross-cup registration per team, not just the regular-division case).

### Full regression check

- `dotnet build Club12-Backend/Solution/Club12.sln` — **0 warnings, 0 errors**.
- `dotnet test Club12-Backend/API.Tests/API.Tests.csproj -c Debug --no-build` — **855/855 passing** (baseline was 851; +4 new `DivisionTeamRegistrationTests`). Zero regressions.

### Files touched this batch

**New:**
- `Club12-Backend/Domain/Entities/Models/DivisionTeamRegistration.cs`
- `Club12-Backend/Infrastructure/Persistance/Configurations/DivisionTeamRegistrationEntityConfiguration.cs`
- `Club12-Backend/Infrastructure/Migrations/20260906005632_AddDivisionTeamRegistrationAndStageDrawnAt.cs` (+ `.Designer.cs`)
- `Club12-Backend/API.Tests/DivisionTeamRegistrationTests.cs`

**Modified:**
- `Club12-Backend/Domain/Entities/Models/Team.cs`
- `Club12-Backend/Domain/Entities/Models/Division.cs`
- `Club12-Backend/Domain/Entities/Models/Stage.cs`
- `Club12-Backend/Domain/Enums/AuditAction.cs`
- `Club12-Backend/Application/DTOs/Stage/Response/StageResponse.cs`
- `Club12-Backend/Infrastructure/Persistance/EntityConstants.cs`
- `Club12-Backend/Infrastructure/Persistance/ApplicationDBContext.cs`
- `Club12-Backend/Infrastructure/Persistance/ApplicationDBContextFactory.cs` (unplanned, required for compile)
- `Club12-Backend/Infrastructure/Migrations/ApplicationDBContextModelSnapshot.cs` (auto-regenerated)
- `Club12-Backend/Infrastructure/Persistance/Seeding/SampleTournamentBuilder.cs`

### Flag for whoever runs Phase 3 (`sdd-apply` batch 2, tasks 3.1–3.14, `DivisionRosterService`)

**Spec/design conflict found, not resolved here (out of this batch's scope):** `specs/division-team-roster/spec.md`'s "Removing a Team From the Roster Is Blocked While It Still Holds Stage Placements" requirement says unenrollment **MUST be rejected** while any `StageTeamMatch` still exists for that team in the division. `design.md` §0 (D7) and `tasks.md` task 3.7 both say the opposite: unenrol **cascades** — deletes the team's `StageTeamMatch` rows first, then the registration (`UnenrollTeamsAsync_TeamStillPlacedInStage_RemovesPlacementThenRegistration`). These cannot both be implemented as written. Whoever picks up Phase 3 (`DivisionRosterService.UnenrollTeamsAsync`) needs this resolved — either the spec needs a delta correction (cascade, not reject) or the design/tasks need to change to a reject-based guard — before writing 3.7's RED test.

### Next recommended (superseded — see Batch 2 below)

~~Continue to Work Unit 2: `DivisionRosterService` + `AssignTeamsToStageAsync` roster-awareness, `tasks.md` Phase 3–4 — after resolving the spec/design conflict noted above.~~

---

## Batch 2 (this run): Work Unit 2 — Phase 3 + Phase 4 + Phase 5 (D2) — tasks 3.1–3.14, 4.1–4.4, 5.1–5.5

Status: **COMPLETE**. Spec/design conflict flagged by batch 1 was resolved before this batch started —
`specs/division-team-roster/spec.md`'s unenrollment requirement was corrected on 2026-09-06 to
**cascade** (delete the team's `StageTeamMatch` row(s) for the division, then the
`DivisionTeamRegistration` row), matching `design.md`/`tasks.md`'s original D7 intent. Implemented as
cascade, not reject.

This batch covers tasks.md's own "Suggested Work Units" table Unit 2 exactly: Phase 3
(`DivisionRosterService`), Phase 4 (`AssignTeamsToStageAsync` roster-awareness), and Phase 5 (D2 —
relax the one-Group-stage invariant). The task brief for this batch explicitly named D2/Phase 5 as
in-scope alongside "Phase 3 and Phase 4," which is what tasks.md's Work Unit 2 also groups together
— treated as authoritative over the stricter "Phase 3–4 only" phrasing elsewhere in the brief.

### Phase 3 — `IDivisionRosterService` / `DivisionRosterService` (3.1–3.14), strict TDD

- New `Club12-Backend/Application/Interfaces/Repositories/IDivisionTeamRegistrationRepository.cs` +
  `Club12-Backend/Infrastructure/Repositories/DivisionTeamRegistrationRepository.cs` — mirrors
  `TeamTournamentRegistrationRepository.cs` exactly (`GenericRepository<DivisionTeamRegistration>`).
  Not in tasks.md's numbered list but required prerequisite scaffolding (same category as batch 1's
  Phase 1) — the design's literal `IGenericRepository<DivisionTeamRegistration>` constructor injection
  was adapted to this codebase's real convention: a typed repository interface exposed through
  `IUnitOfWork`, matching every other repository in `StageService`/`TeamService`.
- `IUnitOfWork`/`UnitOfWork`: added `IDivisionTeamRegistrationRepository DivisionTeamRegistrationRepository`
  property + constructor parameter. **Confirmed no explicit DI registration needed** for either the new
  repository or the new service — `StartupExtensions.HelperRegisterScoped` auto-registers every
  `I{X}Service`/`I{X}Repository` pair by reflection over namespace + suffix convention (verified by
  reading `API/Utils/StartupExtensions.cs:444-464`); task 3.13 is satisfied by this existing mechanism,
  same as batch 1 found for `ApplyConfigurationsFromAssembly`.
- 3.1–3.9 RED, then 3.10–3.11 GREEN — `Club12-Backend/API.Tests/DivisionRosterServiceTests.cs` created
  with the 9 tasked tests plus one bonus test not in the numbered list,
  `UnenrollTeamsAsync_TournamentStructureLocked_Throws` — design says unenroll is "guarded the same way"
  as enroll, and strict TDD forbids adding that guard without a failing test first, so it was added as
  an in-scope extension of Phase 3's own guard-rule work rather than left untested.
  - RED confirmed for the right reason: build failed with `CS0246` (missing `IDivisionRosterService`)
    before the interface existed.
  - `Club12-Backend/Application/Interfaces/Services/IDivisionRosterService.cs` (new) +
    `Club12-Backend/Application/Services/DivisionRosterService.cs` (new) — `GetRosterAsync` (registrations
    joined to `Team`), `EnrollTeamsAsync` (edit-lock guard mirroring `StageService`'s, distinct +
    already-registered skip for idempotency, cross-division-conflict check), `UnenrollTeamsAsync`
    (edit-lock guard, then `StageTeamMatch` delete before `DivisionTeamRegistration` delete — the
    cascade).
  - **Conflict rule implementation, generalized beyond design's literal pseudocode**: design §2.1 only
    wrote the query for "target is NOT cross-cup" (reject a conflicting non-cross registration). The
    spec also requires the symmetric case — a second cross-cup registration is rejected too (`Second
    cross-division-cup registration is rejected`). Implemented as one rule:
    `EnsureNoConflictingRegistrationAsync` rejects when a candidate registration's division has the
    **same** `IsCrossDivisionCup` value as the target division (both regular, or both cross-cup) and is
    a **different** division of the **same** tournament. This produces all three spec scenarios
    correctly: regular+regular conflicts, cross+cross conflicts, regular+cross does not conflict — first
    ran, all 10 tests passed on the first execution, no additional triangulation needed to fix a wrong
    branch.
  - Idempotent-skip precedes the conflict check and only evaluates conflict for genuinely new team ids,
    so re-enrolling an already-registered team never re-triggers the conflict query.
- `ErrorMessages.Division.ConflictingRosterEnrollment(string teamIds)` added (mirrors
  `ErrorMessages.Stage.ConflictingTeamAssignment`'s style and wording pattern).
- GREEN verified: `dotnet test API.Tests/API.Tests.csproj --filter DivisionRosterServiceTests` → 10/10
  passing on first run after the GREEN implementation (no failed-then-fixed iteration needed).

### Phase 4 — `AssignTeamsToStageAsync` roster-aware (4.1–4.4), strict TDD

- 4.1–4.2 RED — extended `Club12-Backend/API.Tests/StageServiceTests.cs`:
  `AssignTeamsToStageAsync_TeamWithNoDivisionRegistration_RejectsThatTeam_CreatesNoStageTeamMatch` and
  `AssignTeamsToStageAsync_AutoMode_OnlyDrawsFromDivisionRoster_NotAllTournamentTeams` (added alongside
  the existing `..._OnlyAssignsTeamsFromStagesTournament`, not replacing it — both now pass and cover
  different axes: tournament-scoping and roster-scoping).
  - RED confirmed genuine: ran before the GREEN change — the no-registration test failed with "No
    exception was thrown" (old code had no membership check at all) and the roster-scoping test failed
    with `Expected: 2, Actual: 4` (old auto branch pulled every tournament-registered team, including
    the other division's).
- 4.3 GREEN — `Club12-Backend/Application/Services/StageService.cs`'s `AssignTeamsToStageAsync`:
  added `_divisionTeamRegistrationRepository` field (via `unitOfWork.DivisionTeamRegistrationRepository`);
  new private `EnsureTeamsEnrolledInDivisionAsync` throws
  `ErrorMessages.Stage.TeamNotEnrolledInDivision(missingIds)` (new message) for the manual path, called
  right after computing `filteredIds` and before the slot-capacity math, per design §2.2's ordering. The
  `auto` branch's candidate query changed from `team.TournamentId == stage.Division.TournamentId` to
  `team.DivisionTeamRegistrations.Any(r => r.DivisionId == stage.DivisionId)` — using the `Team` nav
  collection added in batch 1 directly, simpler than design's literal repository-injection sketch and
  consistent with the existing `!team.StageTeamMatches.Any(...)` style in the same query.
  `EnsureNoCrossDivisionConflictAsync` kept unchanged (belt-and-suspenders per design §1.4).
  Updated the stale XML doc comments on `CreateStageAsync` in both `StageService.cs` and
  `IStageService.cs` ahead of Phase 5 touching the same method (see below) — not duplicated effort,
  done once.
- 4.4 — Updated **three** existing test files' seed helpers to register teams before a successful
  `AssignTeamsToStageAsync` call, since the new membership precondition would otherwise reject them.
  tasks.md named only `StageServiceTests.cs` lines 429-867; two more files needed the same fix, found by
  running the full suite after the GREEN change and reading each failure's stack trace — not found by
  grep in advance, which is itself a lesson for future roster-precondition work: **grep for
  `AssignTeamsToStageAsync(` calls across the whole test project, not just the file tasks.md names,
  before declaring a roster-precondition change complete.**
  - `StageServiceTests.cs`: `SeedTeamsAsync` gained an optional `Division? registerToDivision = null`
    parameter that also inserts a `DivisionTeamRegistration` per team when provided;
    `SeedStageWithSlotsAsync`'s existing-assignment loop now also registers those teams (keeps the new
    "every `StageTeamMatch` implies a registration" invariant true for older helper-seeded data, not
    just new call sites). Six call sites updated to pass `stage.Division` (or a same-tournament
    `Division`) where the test expects `AssignTeamsToStageAsync` to succeed.
  - `Club12-Backend/API.Tests/StageTeamAssignmentConsistencyTests.cs` (not named in tasks.md) — all
    three tests call `AssignTeamsToStageAsync` manually without ever going through
    `DivisionRosterService`; added a `SeedRegistrationAsync` helper and registered the team into every
    division it gets assigned to in each test, preserving each test's real intent (the belt-and-suspenders
    cross-division-conflict check in `StageService` itself, independent of the roster layer).
  - `Club12-Backend/API.Tests/CrossCupMultiGroupBracketGenerationTests.cs` (not named in tasks.md) —
    `BuildAndPlayGroupAsync`'s freshly-seeded teams are now also registered to the division right after
    seeding, before `CreateStageAsync`/`AssignTeamsToStageAsync` run.
- GREEN verified: `dotnet test API.Tests/API.Tests.csproj --filter StageServiceTests` → 30/30 passing;
  full suite confirmed green after the three-file fix (see Full regression check below).

### Phase 5 — Relax the one-Group-stage-per-division invariant [D2] (5.1–5.5), strict TDD

- 5.1 RED — added `CreateStageAsync_RegularDivision_AllowsSecondGroupStageWithDistinctName` to
  `StageServiceTests.cs`. RED confirmed genuine: ran before the GREEN change, failed with the exact old
  exception message ("Esta división ya tiene una fase de grupos...").
- 5.2 — Deleted (not merely rewritten) `CreateStageAsync_DivisionAlreadyHasGroupStage_ThrowsEvenWithDifferentName`
  and replaced it in place with the 5.1 test, per tasks.md's explicit instruction that D2 makes the old
  assertion permanently false. `CreateStageAsync_DivisionAlreadyHasGroupStage_StillAllowsNonGroupStage`
  and `CreateStageAsync_CrossDivisionCup_AllowsSecondGroupStage` left untouched — both still true under
  D2, both still passing.
- 5.3 GREEN — `Club12-Backend/Application/Services/StageService.cs`'s `CreateStageAsync`: deleted the
  entire `if (stageEntity.StageType == StageType.Group) { hasGroupStage / isCrossDivisionCup / throw }`
  block. The duplicate-**name** guard (`ErrorMessages.Stage.AlreadyExistsInDivision`, pre-existing,
  untouched) is what remains and is what task 5.3's design note calls "the check that replaced it" —
  more precisely, no new check replaced the removed one; the pre-existing name-collision guard, which
  already ran earlier in the same method, is now the only remaining validation on a division's Group
  stages. It still rejects a genuinely accidental duplicate (same name) while allowing legitimately
  distinct sub-groups ("Grupo A", "Grupo B", ...). **This is not "no validation" — it is exactly the
  validation design.md prescribes**: D2's own text says "the duplicate-name guard already prevents true
  accidental duplicates... this also satisfies the split-league forward-compat flag," explicitly framing
  the name guard as sufficient, not a gap.
- 5.4 — Grepped `GroupStageAlreadyExistsInDivision` across the whole backend before touching it: exactly
  3 hits (the constant's own definition, the `StageService.cs` throw just deleted, and the test just
  deleted). After the above edits, zero real callers remained, so — unlike task 5.4's "or leave it
  unused" option — the now-fully-dead `ErrorMessages.Stage.GroupStageAlreadyExistsInDivision` constant
  was deleted outright, consistent with this codebase's demonstrated preference for removing rather than
  stockpiling dead constants (see the HU-124 removal phase's own `TournamentBracketSize` deletion).
- 5.5 — Verified: 5.1 passes; full `StageServiceTests` class green (30/30, same run as Phase 4's
  verification since both phases' tests live in the same file and were verified together).

### Full regression check

- `dotnet build Club12-Backend/Solution/Club12.sln` — **0 warnings, 0 errors**.
- `dotnet test Club12-Backend/API.Tests/API.Tests.csproj -c Debug --no-build` — **867/867 passing**.
  Baseline was 855 (end of batch 1). Net +12: +10 `DivisionRosterServiceTests`, +2 net in
  `StageServiceTests` (+2 Phase 4 tests, +1 Phase 5 test, -1 Phase 5 deleted test). Zero regressions —
  every one of the 855 pre-existing tests still passes, including the 3 tests in unrelated files
  (`StageTeamAssignmentConsistencyTests.cs`, `CrossCupMultiGroupBracketGenerationTests.cs`) whose seed
  helpers needed updating for the new roster precondition to keep passing for their original reason.

### Files touched this batch

**New:**
- `Club12-Backend/Application/Interfaces/Repositories/IDivisionTeamRegistrationRepository.cs`
- `Club12-Backend/Infrastructure/Repositories/DivisionTeamRegistrationRepository.cs`
- `Club12-Backend/Application/Interfaces/Services/IDivisionRosterService.cs`
- `Club12-Backend/Application/Services/DivisionRosterService.cs`
- `Club12-Backend/API.Tests/DivisionRosterServiceTests.cs`

**Modified:**
- `Club12-Backend/Application/Interfaces/Repositories/IUnitOfWork.cs`
- `Club12-Backend/Infrastructure/Repositories/UnitOfWork.cs`
- `Club12-Backend/Application/Utils/Constants/ErrorMessages.cs` (added `Division.ConflictingRosterEnrollment`,
  `Stage.TeamNotEnrolledInDivision`; removed the now-fully-dead `Stage.GroupStageAlreadyExistsInDivision`)
- `Club12-Backend/Application/Services/StageService.cs` (`AssignTeamsToStageAsync` roster precondition +
  roster-scoped auto branch; `CreateStageAsync` invariant removal; stale XML comment fixes on both)
- `Club12-Backend/Application/Interfaces/Services/IStageService.cs` (stale XML comment fix)
- `Club12-Backend/API.Tests/StageServiceTests.cs` (2 new Phase 4 tests, 1 new/1 deleted Phase 5 test,
  `SeedTeamsAsync`/`SeedStageWithSlotsAsync` helper updates, 6 call-site updates)
- `Club12-Backend/API.Tests/StageTeamAssignmentConsistencyTests.cs` (registration seeding added to all
  3 tests, not named in tasks.md)
- `Club12-Backend/API.Tests/CrossCupMultiGroupBracketGenerationTests.cs` (registration seeding added to
  `BuildAndPlayGroupAsync`, not named in tasks.md)

### Next recommended (superseded — see Batch 3 below)

~~Continue to Work Unit 3: playoffs-only draw preview/commit/guard/audit, `tasks.md` Phase 6–8.~~

---

## Batch 3 (this run): Work Unit 3 — Phase 6 — tasks 6.1–6.21

Status: **COMPLETE**. All tasks 6.1 through 6.21 implemented, TDD-verified, zero regressions.

This batch covers exactly `tasks.md`'s Phase 6 ("Playoffs-only draw — preview + commit + re-draw
guard"), matching the Suggested Work Units table's Unit 3 row (`PlayoffDrawTests` filter). The task
brief for this batch referred to it as "Phase 6, 7, and 8" but its own concrete description (preview,
commit, re-draw guard, audit) and its `next_recommended` pointer (sub-group rebuild + HU-124 removal)
both resolve unambiguously to `tasks.md`'s actual Phase 6 alone — Phase 7 (sub-group rebuild) and
Phase 8 (completability validator) are untouched, reserved for the next batch (`tasks.md`'s real Unit
4, Phases 7–9).

- 6.1 — `Club12-Backend/Domain/Enums/DrawMode.cs` (new) — `Random`, `Manual`.
- 6.2 — New DTOs (one type per file): `Application/DTOs/Stage/Request/DrawRequest.cs`,
  `Application/DTOs/Stage/Response/DrawPairPreview.cs`, `.../DrawPreviewResult.cs`.
- 6.3–6.12 RED — `Club12-Backend/API.Tests/PlayoffDrawTests.cs` created with 11 tests (9 tasked names,
  with 6.11's "audit entry describes draw mode" written as a 2-case `[Theory]` over Random/Manual
  rather than a second untasked test name, and 6.7's "invalid or mismatched" covered as two assertions
  in one test method per its own singular task name): preview persists nothing and returns a token;
  preview rejected when the division has a group phase; a 6-team roster produces 2 byes via the
  unchanged `PlayoffSeeder.SeedPairs`; a valid token's commit matches its preview exactly; a garbage
  token and a token minted for a different stage are both rejected; manual order seeds verbatim with no
  shuffle (asserted against a fresh `PlayoffSeeder.SeedPairs` call over the same manual order); a
  3-team bye advances into the next round via the existing `TryAdvanceStageWinnerAsync`; `DrawnAt` is
  stamped on the first-round stage only, never later rounds; the audit entry's `Detail` names the mode
  and team count; and an audit-service failure never blocks the draw.
  - RED confirmed genuine: build failed with `CS0535` (`StageService` does not implement
    `IStageService.PreviewDrawAsync`/`CommitDrawAsync`) before either method existed — the interface
    additions (6.20's signatures) were added first specifically to force this failure, then the test
    file, then the implementation.
  - Test 6.12 (`CommitDrawAsync_AuditServiceThrows_DrawStillSucceeds`) needed a throwing `IAuditService`
    double since this codebase uses no mocking library (`TEST-001`): a private nested
    `ThrowingAuditService : IAuditService` inside the test file, with `StageService` constructed
    manually from the same DI scope's `IUnitOfWork`/`ILogger`/`IConfiguration` plus the throwing double
    — the same "construct the service by hand with one substituted dependency" pattern already
    established by `API.Tests/Backup/Fakes/FakeAuditService.cs` for `BackupOperationsServiceTests`, not
    a new convention.
- 6.13–6.17 RED — `Club12-Backend/API.Tests/BracketRedrawGuardTests.cs` created with 6 tests (5 tasked
  names, 6.14's three independent "played" triggers — `IsFinished`, a recorded score, `Status.Played`
  — written as one `[Theory]` with `TheoryData` rather than three separate test methods): no played
  matches allows the draw; each of the three played-triggers independently blocks a re-draw; a bye
  match, `VisitorTeamId == null`, never counts as played, so a freshly-drawn bracket stays re-drawable;
  two parallel brackets under different `BracketName` values in the same division lock independently
  (Copa de Oro locked, Copa de Plata still drawable); and a `BestOf=3` re-draw before anything is played
  deletes the prior `MatchSeries` rows and creates fresh ones for the new pairing.
  - Since `EnsureBracketDrawableAsync` is private, every guard scenario is exercised through
    `CommitDrawAsync` itself using `DrawMode.Manual` — the same "test a private guard via the public
    method that calls it" convention already used for `EnsureDivisionStructureEditableAsync` elsewhere
    in `StageServiceTests.cs`.
- 6.18 GREEN — `Club12-Backend/Application/Utils/Constants/ErrorMessages.cs`: added
  `Stage.DrawRequiresGrouplessDivision`, `Stage.InvalidDrawToken`, `Stage.ManualOrderNotRosterPermutation`,
  `Stage.BracketAlreadyPlayed`. No separate "not enough ranked teams" message was added — `PlayoffSeeder
  .SeedPairs`'s own existing `< 2` guard (`ErrorMessages.Playoff.NotEnoughRankedTeams`) already covers
  that case for both preview and commit, so duplicating it was unnecessary.
- 6.19 GREEN — `EnsureBracketDrawableAsync` implemented in `StageService.cs` exactly per design §2.5's
  query shape: scoped to `(Stage.DivisionId, Stage.BracketName)`, requires both `HomeTeamId` and
  `VisitorTeamId` set before any of `IsFinished`/`HomeScore`/`VisitorScore`/`Status == Played` counts —
  this is the D4 bye-exclusion the batch brief flagged as the critical detail, and it is implemented,
  not missed (verified live by `EnsureBracketDrawableAsync_ByeMatchesDoNotCountAsPlayed`, which commits
  a 3-team draw — producing a real bye with `IsFinished = true` — and then successfully re-draws the
  same bracket before anything else is played).
- 6.20 GREEN — `PreviewDrawAsync`/`CommitDrawAsync` implemented on `IStageService`/`StageService`:
  - **Token**: base64url `{payload}.{signature}`, payload is `JsonSerializer.SerializeToUtf8Bytes` of a
    private nested `DrawTokenPayload { StageId, OrderedTeamIds, IssuedAtUtc, Nonce }`, signature is
    `HMACSHA256` over the payload segment's own base64url bytes using
    `configuration.GetSection(ConfigurationKeys.Jwt.Key).Value` — the exact reused secret confirmed by
    `tasks.md`, no new configuration key. `CryptographicOperations.FixedTimeEquals` compares the
    signature to prevent a timing side-channel. `StageService`'s constructor gained `IConfiguration
    configuration` and `IAuditService auditService` parameters; both were already registered in DI for
    other consumers, so no `StartupExtensions` change was needed.
  - **Manual mode validation**: `IsRosterPermutation` (set-equality plus count-equality, so no team is
    missing, repeated, or foreign) validates `manualOrder` against the live division roster for both
    preview and commit, and the same helper re-validates a Random token's `OrderedTeamIds` against the
    roster at commit time — per design D3, meaning a token becomes naturally invalid if the roster
    changes between preview and commit, not just on signature mismatch.
  - **Reset step (D5)**: `ResetBracketSeedingAsync` nulls every bracket-stage match's
    `HomeTeamId`/`VisitorTeamId`/`WinningTeamId`/scores, sets `IsFinished = false` and
    `Status = Scheduled`, clears `SeriesId`/`GameNumber`, persists that via `UpdateRangeAsync` **before**
    bulk-deleting the now-orphaned `MatchSeries` rows via `RemoveAsync` — that ordering matters because
    `RemoveAsync` uses `ExecuteDeleteAsync` (bypasses the change tracker), so the FK-referencing columns
    must already be nulled and saved first. No-op on an initial draw since there is nothing to reset.
  - Reuses `PlayoffSeeder.SeedPairs` and the existing private `FillStageWithSeedsAsync` unchanged, per
    the proposal's explicit instruction; stamps `DrawnAt` only on the first-round stage passed in, calls
    the existing `TryAdvanceStageWinnerAsync`, then `LogPlayoffDrawAsync` (audit, see below).
  - **Audit (task's item 4)**: `LogPlayoffDrawAsync` wraps `IAuditService.LogAsync(AuditAction
    .PlayoffDraw, targetType: "Stage", targetId: firstRoundStage.Id.ToString(), targetName:
    firstRoundStage.Name, detail: "Sorteo aleatorio/manual - {N} equipos")` in a try/catch that logs via
    `ILogger.LogWarning` and never rethrows — **required**, not redundant: unlike the design note's
    assumption, `IAuditService.LogAsync`/`AuditService.LogAsync` does **not** swallow its own exceptions
    (confirmed by reading `AuditService.cs` — a straight `await auditLogRepository.AddAsync(...)` with
    no try/catch), so the call-site try/catch in `StageService` is what actually satisfies
    `CommitDrawAsync_AuditServiceThrows_DrawStillSucceeds` and the ERROR-001 "never silently swallow"
    rule (logged via `ILogger`, not discarded).
- 6.21 — Verified: `dotnet test API.Tests/API.Tests.csproj --filter PlayoffDrawTests|BracketRedrawGuardTests`
  → 18/18 passing.

### Consistency check requested by the user: "preview repeatedly, commit once"

Confirmed supported by the token design as built, not merely "preview once, commit immediately":
`PreviewDrawAsync` is fully stateless and side-effect-free (asserted directly by
`PreviewDrawAsync_GrouplessDivision_ReturnsPairsAndToken_PersistsNothing` — no `StageTeamMatch`/`Match`/
`Stage.DrawnAt` change afterward), and every call mints an independent, self-contained, signed token
with its own `Nonce`/`IssuedAtUtc` — nothing server-side is invalidated or consumed by issuing a new
preview. An organizer running a live "sorteo" can call preview as many times as they want to re-roll,
then call commit exactly once with whichever previewed token they liked, and the committed bracket is
guaranteed identical to that specific preview (verified by `CommitDrawAsync_ValidToken_BracketMatchesPreview`).
No artificial expiry window was added — design.md's prose mentions "expired" as one of several adjectives
for a bad token, but no TTL value is specified anywhere in spec.md, design.md, or tasks.md, and no task
or spec scenario tests one; inventing an unrequested, undocumented expiry would violate strict TDD (no
failing test drives it) and could actively break a legitimately slow live-draw ceremony between preview
and commit. The one real "staleness" trigger implemented is roster-set drift: since commit re-validates
`OrderedTeamIds` against the division's live roster, a token from before a roster change naturally fails
`IsRosterPermutation` and is rejected as a mismatched token — an organic consequence of the design, not
a separate feature.

### Full regression check

- `dotnet build Club12-Backend/Solution/Club12.sln` — **0 warnings, 0 errors**.
- `dotnet test Club12-Backend/API.Tests/API.Tests.csproj -c Debug --no-build` — **885/885 passing**.
  Baseline was 867 (end of batch 2). Net +18: 11 `PlayoffDrawTests` + 7 `BracketRedrawGuardTests`. Zero
  regressions — every one of the 867 pre-existing tests still passes.

### Files touched this batch

**New:**
- `Club12-Backend/Domain/Enums/DrawMode.cs`
- `Club12-Backend/Application/DTOs/Stage/Request/DrawRequest.cs`
- `Club12-Backend/Application/DTOs/Stage/Response/DrawPairPreview.cs`
- `Club12-Backend/Application/DTOs/Stage/Response/DrawPreviewResult.cs`
- `Club12-Backend/API.Tests/PlayoffDrawTests.cs`
- `Club12-Backend/API.Tests/BracketRedrawGuardTests.cs`

**Modified:**
- `Club12-Backend/Application/Interfaces/Services/IStageService.cs` (`PreviewDrawAsync`/`CommitDrawAsync`
  signatures)
- `Club12-Backend/Application/Services/StageService.cs` (constructor gained `IConfiguration`/
  `IAuditService`; `PreviewDrawAsync`, `CommitDrawAsync`, `EnsureBracketDrawableAsync`,
  `ResetBracketSeedingAsync`, `LogPlayoffDrawAsync`, token sign/verify helpers, `DrawTokenPayload`)
- `Club12-Backend/Application/Utils/Constants/ErrorMessages.cs` (4 new `Stage` messages)

### Next recommended (superseded — see Batch 4 below)

~~Continue to Work Unit 4: sub-group rebuild (HU-123) + balanced distribution (HU-121/122) +
`TournamentCompletabilityValidator` extension + HU-124 dead-endpoint removal, `tasks.md` Phase 7–9.~~

---

## Batch 4 (this run): Work Unit 4 — Phase 7 + Phase 8 (backend) + Phase 9 — tasks 7.1–7.13, 8.1–8.5/8.7, 9.1–9.7

Status: **COMPLETE** (backend scope). Task 8.6 (frontend Spanish label for the new completability
issue code) is explicitly deferred to Work Unit 5 — this batch's brief was backend-only. All other
tasks in Phase 7–9 implemented, TDD-verified, zero regressions.

### Phase 7 — Balanced distribution (HU-121) + manual reassignment (HU-122) + sub-group rebuild
(HU-123), strict TDD

- New `Club12-Backend/Application/Utils/Helper/SubGroupDistribution/SubGroupDistribution.cs` — pure
  static helper, no DB/service dependency: `MinTeamsPerSubGroup = 4` (the single canonical source of
  this constant, reused by the completability validator, `RebuildSubGroupsAsync`, and
  `ReassignTeamToSubGroupAsync` — deliberately not duplicated per design §2.8's literal "new constant"
  text), `MeetsMinimumSize(int totalTeams, int subGroupCount)` (treats an empty roster as always
  valid, per spec's "editing before enrolment" case), and `Distribute(IReadOnlyList<Guid>
  rosterTeamIds, int subGroupCount)` (Fisher-Yates shuffle then round-robin deal, guaranteeing a
  max-min group-size gap of at most 1).
  - RED confirmed genuine: `API.Tests/SubGroupDistributionTests.cs` (6 tests) failed to compile with
    `CS0234` (namespace `Application.Utils.Helper.SubGroupDistribution` did not exist) before the
    helper was written.
  - GREEN first try, no triangulation-driven fix needed: 6/6 passed immediately after the helper was
    implemented (16→3 groups is 5/5/6; 16→4 is 4/4/4/4; permutation-preservation via sorted-set
    equality; three `MeetsMinimumSize` cases including the empty-roster skip).
  - **Namespace-collision gotcha worth remembering**: naming the class the same as its containing
    namespace segment (`Application.Utils.Helper.SubGroupDistribution.SubGroupDistribution`, mirroring
    this codebase's existing `StageHelper`/`StageHelper` precedent) breaks when the *consuming* file's
    own namespace is a **sibling** of that namespace under the same parent (here,
    `TournamentCompletabilityValidator.cs` lives in `Application.Utils.Helper.Tournament`, a sibling of
    `Application.Utils.Helper.SubGroupDistribution`). In that specific case C# resolves the bare name
    `SubGroupDistribution` to the sibling *namespace* via enclosing-namespace lookup, not the type, even
    with an explicit `using` — `SubGroupDistribution.MinTeamsPerSubGroup` then fails with CS0234 as if
    the member doesn't exist. Fixed both call sites (`TournamentCompletabilityValidator.cs`,
    `StageService.cs`) with a type alias importing the fully-qualified type under a different name:
    `using SubGroupDistributionHelper = Application.Utils.Helper.SubGroupDistribution.SubGroupDistribution;`.
    `StageServiceTests.cs`'s and other consumers not in a sibling namespace of
    `Application.Utils.Helper.SubGroupDistribution` never hit this and don't need the alias.
- 7.4–7.9, 7.12 RED then GREEN — `Club12-Backend/API.Tests/SubGroupRebuildTests.cs` created (its own
  self-contained `SeedTournamentAsync`/`SeedDivisionAsync`/`SeedRosterAsync` helpers, not shared with
  `StageServiceTests.cs`'s private helpers) with 10 tests total (5 tasked + `AutoDistributeRosterAsync`
  + 2 `ReassignTeamToSubGroupAsync` tests + the HU-125 rebuild-path fence test, see Phase 9 note below).
  - RED confirmed genuine: build failed with `CS0535` (`StageService` did not implement
    `IStageService.RebuildSubGroupsAsync`/`AutoDistributeRosterAsync`/`ReassignTeamToSubGroupAsync`)
    before any of the three methods existed — interface signatures were added first specifically to
    force this failure.
  - GREEN: 9/10 passed on the very first implementation run; the one failure
    (`ReassignTeamToSubGroupAsync_MovesTeam_OtherPlacementsUnchanged`) was a **test-design bug, not a
    production bug** — it seeded 12 teams into 3 groups of exactly 4 (the minimum) and then tried to
    move a team away, which the min-4 guard correctly rejected. Fixed by reseeding 15 teams/3 groups (5
    each, so the move leaves the source at exactly the floor of 4, still a valid boundary test) — after
    that fix, 10/10 passed. This is exactly the kind of "RED for the wrong reason inside GREEN" strict
    TDD wants caught, not silently special-cased.
  - `IStageService`/`StageService`: `RebuildSubGroupsAsync(Guid divisionId, int subGroupCount)` —
    guards (positive count; `EnsureDivisionStructureEditableAsync`; HU-125 fence, see below;
    `SubGroupDistribution.MeetsMinimumSize` skipped only when the roster is empty) — then deletes every
    existing `Group`-type `Stage` for the division via a single `_stageRepository.RemoveAsync`
    (confirmed DB-level `OnDelete(DeleteBehavior.Cascade)` on both `StageTeamMatch.Stage`
    — explicit — and `Match.Stage` — EF's default cascade for a required FK, verified by reading
    `MatchEntityConfiguration.cs`, which has no explicit `.OnDelete()` call — so cascading to Matches +
    StageTeamMatch needs no manual pre-delete step, contrary to a literal reading of design's "delete
    ... their Matches + StageTeamMatch rows" as if that were a separate step), builds `G` new stages via
    the existing private `BuildStage` helper (reused, not reinvented) named "Grupo A".."Grupo {G}",
    slugs via the existing `AssignStageSlugsAsync`, `AddRangeAsync`, then deals the untouched roster via
    the new `PlaceRosterIntoSubGroupsAsync` helper (wraps `SubGroupDistribution.Distribute` +
    `StageTeamMatch` creation). Roster (`DivisionTeamRegistration`) is never read for count only — it is
    the literal team-id source for the new placements, never deleted or re-created; only
    `StageTeamMatch`/`Stage` rows are destroyed and rebuilt, proven by
    `RebuildSubGroupsAsync_RosterUnchanged_AcrossCountChange` asserting the exact same 16
    `DivisionTeamRegistration.Id` values survive a 3→4 sub-group count change.
  - `AutoDistributeRosterAsync(Guid divisionId)` [D9] — clears every `StageTeamMatch` row of the
    division's existing `Group` stages (no-op if none exist) and re-deals the full roster via the same
    `PlaceRosterIntoSubGroupsAsync` helper — always balanced, never fill-only-empties, proven by
    `AutoDistributeRosterAsync_ClearsThenRedistributes_AlwaysBalanced` (deliberately collapses all 16
    teams into one group first, then asserts a clean 4/4/4/4 split afterward).
  - `ReassignTeamToSubGroupAsync(Guid teamId, Guid fromStageId, Guid toStageId)` [D10, new design
    decision — design.md sketched `RebuildSubGroupsAsync`/`AutoDistributeRosterAsync` but not a
    single-team manual move; this method's shape was designed during this batch to satisfy spec.md's
    "Manual Team-to-Subgroup Reassignment Always Available" requirement] — validates both stages exist
    and belong to the same division, guards the edit lock, confirms the team is actually placed in
    `fromStageId`, then checks **only** that the source sub-group would not drop below
    `SubGroupDistribution.MinTeamsPerSubGroup` after the move — no destination-size cap, no rebalance
    trigger, no other restriction, per the batch brief's explicit "do not invent additional
    restrictions" instruction. Implemented as a direct `StageTeamMatch.StageId` mutation (preserves the
    row's `Id`/`CreatedBy`/`DateCreated`), not a delete+recreate.
    `ReassignTeamToSubGroupAsync_ArbitraryMoveAboveMinimum_Allowed` proves this genuinely allows an
    arbitrary move (source 6→5, destination 6→7, an intentionally unbalancing move) succeeds as long as
    the floor holds; `ReassignTeamToSubGroupAsync_WouldDropSourceBelowMinimum_Rejected` proves the one
    hard constraint still bites (2 groups of exactly 4, moving away from either is rejected).
- New `ErrorMessages.Stage` members: `SubGroupCountMustBePositive`, `TeamNotPlacedInSubGroup`,
  `ReassignmentAcrossDivisionsNotAllowed`, `SubGroupsIncompatibleWithPositionRangeCups`,
  `SubGroupTooFewTeams(int teamCount, int subGroupCount)`,
  `SubGroupReassignmentBelowMinimum(int remainingTeams)`.

### HU-125 scope fence — rejection point, exact enforcement, and why (tasks 7.13, cross-referenced
from Phase 9)

**Spec requirement**: `specs/stage-generation/spec.md`'s "Sub-Groups Combined With Position-Range Cups
Are Rejected, Not Silently Miscalculated" — two directions: (1) enabling `subGroupCount >= 2` when a
position-range `DivisionPlayoffMapping` already exists must be rejected; (2) configuring a
position-range cup when `subGroupCount >= 2` already exists must be rejected. Both are **request-time
hard rejections** ("THEN the request is rejected"), not a completability warning surfaced later — this
is why the fence was NOT folded into Phase 8's `TournamentCompletabilityValidator` extension despite
the batch brief's loose phrasing grouping them together; the validator only fires at tournament-start
time, which is too late for what spec.md's Given/When/Then scenarios actually demand.

**Investigated the codebase before deciding the enforcement points** (this exact investigation is the
deliverable the batch brief asked for — "determine the exact rejection point... from spec.md"):
`DivisionPlayoffMapping` rows can currently be set on a `Division` **only** at that division's creation
moment, via either `CreateDivisionRequest` (standalone `DivisionService.CreateDivisionAsync`, no
`Stages` field — stages are always added afterward, one `CreateStageAsync` call at a time) or
`CreateFullDivisionRequest` (`TournamentService.CreateDivisionWithStagesAsync`, the wizard's
all-in-one-request path). There is **no** existing endpoint anywhere in the backend that edits
`PlayoffMappings` on an already-existing division (`UpdateDivisionRequest` has no `PlayoffMappings`
field at all — confirmed by grep). Consequence: direction (2)'s literal scenario — "a division already
has 2+ sub-groups, THEN configuring a cup is rejected" — is **structurally unreachable** in this
codebase today, because by the time any division can legally reach `subGroupCount >= 2` (via repeated
`CreateStageAsync` calls or via `RebuildSubGroupsAsync`), there is no code path left that could still
attach a fresh `DivisionPlayoffMapping` to it. This is not a shortcut taken to save effort — it is the
honest state of the codebase's real capabilities, and is exactly why direction (2) is satisfied
vacuously rather than by a dedicated new guard; inventing an endpoint to exercise an otherwise-dead
code path would violate strict TDD's "no failing test drives it" rule.

**What is actually load-bearing and tested**, two guards implementing direction (1) against the two
real ways a division can gain a 2nd sub-group:
- `StageService.CreateStageAsync`: when `stageEntity.StageType == StageType.Group`, a new private
  `EnsureSubGroupCupCompatibilityAsync(Guid divisionId)` loads the division with `PlayoffMappings`,
  and — only when the division is **not** cross-division-cup and already has ≥1 `PlayoffMappings` row —
  checks whether a `Group` stage already exists for it; if so, throws
  `ErrorMessages.Stage.SubGroupsIncompatibleWithPositionRangeCups`. This covers the manual/incremental
  path (an admin or the wizard's per-stage loop calling `CreateStageAsync` twice for the same division).
  Proven by `CreateStageAsync_SecondGroupStage_RejectedWhenDivisionHasPositionRangeCup`
  (`Club12-Backend/API.Tests/StageServiceTests.cs`) — seeds a division with 1 existing `Group` stage
  plus a `DivisionPlayoffMapping`, asserts the 2nd `CreateStageAsync` call throws and the stage count
  stays at 1.
- `StageService.RebuildSubGroupsAsync`: rejects outright when `subGroupCount >= 2 &&
  !division.IsCrossDivisionCup && division.PlayoffMappings.Count > 0`, **before** deleting any existing
  stage or touching the roster. This covers the bulk-rebuild path, which by design bypasses
  `CreateStageAsync`'s guard entirely (`AddRangeAsync` direct, per design §2.7). Proven by
  `RebuildSubGroupsAsync_MultipleSubGroups_RejectedWhenDivisionHasPositionRangeCup`
  (`Club12-Backend/API.Tests/SubGroupRebuildTests.cs`) — asserts the exception is thrown and zero
  `Stage` rows exist for the division afterward (no partial destroy-then-fail).

**Exact error message** (`ErrorMessages.Stage.SubGroupsIncompatibleWithPositionRangeCups`, both guards
share the same constant): "No se pueden combinar sub-grupos con una copa configurada por rango de
posiciones: la tabla de posiciones combinada no está definida para varios sub-grupos independientes.
Usá un solo sub-grupo o quitá el mapeo de playoff antes de continuar." — names the actual reason (no
combined-table definition across sub-groups) and tells the organizer the two ways out, per the batch
brief's "a real tournament organizer needs to understand this" instruction.

**This satisfies**: `specs/stage-generation/spec.md`'s "Sub-Groups Combined With Position-Range Cups
Are Rejected, Not Silently Miscalculated" requirement — scenario 1 ("Enabling sub-groups is rejected
when a position-range cup already exists") fully, both scenario paths (manual/incremental and rebuild);
scenario 2 ("Configuring a position-range cup is rejected when sub-groups already exist") vacuously,
for the structural reason explained above; scenario 3 ("Single sub-group is unaffected") is a natural
consequence of both guards' `>= 2`/`existing stage` conditions never firing for `G == 1`.

### Phase 8 — `TournamentCompletabilityValidator` extension (backend only this batch), strict TDD

- 8.1–8.3 RED, 8.5 GREEN — extended `Club12-Backend/API.Tests/TournamentCompletabilityValidatorTests.cs`
  with a new `AddZoneWithSubGroups` in-memory graph builder (one `Division` with N `Group` stages, each
  pre-seeded with its own teams) and 3 tests. RED confirmed genuine: 2 of the 3 new tests failed with
  "collection did not contain any matching items" (no rule existed yet to fire the new issue code) while
  the 3rd (`Validate_SubGroupsBalancedAndAboveMinimum_NoIssue`) passed trivially for the same reason —
  exactly the "GREEN that passes for the wrong reason" case strict TDD's evidence table exists to catch,
  noted here rather than silently accepted.
  - `TournamentCompletabilityValidator.Validate`: for every regular division with `> 1` `Group` stages,
    computes each sub-group's distinct assigned-team count, and fires
    `CompletabilityIssueCodes.SubGroupTooFewTeams` when the smallest sub-group is below
    `SubGroupDistribution.MinTeamsPerSubGroup` **or** the max-min gap is `>= 2` — one rule covering both
    the "too small" and "hand-edited imbalance" scenarios per design §2.8, reusing Phase 7's helper
    constant rather than duplicating it (see the namespace-alias note above for why a type alias was
    needed at this call site specifically).
  - GREEN: 14/14 `TournamentCompletabilityValidatorTests` passed after the implementation, no further
    triangulation needed beyond the 3 written cases (below-minimum, balanced-no-issue,
    hand-edited-imbalance) since design's rule is a single two-branch condition and both branches are
    directly exercised.
- 8.4 — added `CompletabilityIssueCodes.SubGroupTooFewTeams`; deliberately did **not** add a second
  `MinTeamsPerSubGroup` constant on the validator as design's literal text says — reused
  `SubGroupDistribution.MinTeamsPerSubGroup` (Phase 7) as the one canonical source, since two constants
  with the same value and no enforced link would silently drift if one changed and not the other.
- 8.6 — **explicitly deferred to Work Unit 5.** This batch's brief is backend-only ("Do not touch
  frontend files"); the frontend `completabilityMessages` Spanish-label map lives in
  `Club12-WebClient`. Flagged as an open item for whoever runs the frontend batch.

### Phase 9 — HU-124 dead-endpoint removal, per tasks.md's confirmed-dead-code analysis

- Deleted `StageService.CreateAutomatedStagesAsync` (the whole method, ~75 lines) and the private
  `IsValidTournamentSize` helper (its only caller). Deleted
  `Club12-Backend/Application/Utils/Constants/Stage/TournamentBracketSize.cs` entirely (`rm`, not just
  emptied). Left `MaxTeams.Group` and `StageHelper.cs`'s `StageType.Group => MaxTeams.Group` switch arm
  untouched, exactly as tasks.md's confirmations-resolved section specified — confirmed still correct by
  grep before finishing (see below).
- Deleted `CreateAutomatedStagesAsync` from `IStageService.cs`.
- Deleted `StageController.GenerateStagesAndMatches` and its `[HttpPost("generate/{id:guid}")]` route.
  **Unplanned but required cleanup, not in tasks.md's literal list**: this method was the *only* user of
  the `matchService` primary-constructor parameter on `StageController` — grepped the whole file to
  confirm before removing `IMatchService matchService` from the constructor entirely (an unused
  primary-constructor parameter is silent, not a compiler warning, so `dotnet build`'s 0-warnings gate
  would not have caught this on its own; removing it anyway matches DOTNET_STANDARDS' spirit even where
  the analyzer is silent). Updated the controller's stale XML class summary ("automated generation" no
  longer exists as a capability) per XML-004.
- Deleted the five `CreateAutomatedStagesAsync_*` characterization tests from `StageServiceTests.cs`
  (`_EightTeams_CreatesTwoGroupsWithoutQuarterFinal`, `_ValidSizesWithQuarterFinal_...` theory,
  `_InvalidTeamCount_ThrowsAndCreatesNoStages` theory, `_DivisionNotFound_Throws`,
  `_DivisionAlreadyHasStages_Throws` — 8 individual test executions total across the 2 theories) and
  rewrote the class's stale XML summary. **Also removed the now-fully-dead
  `SeedTournamentWithTeamsAsync` test helper and the unused `ValidSizesWithQuarterFinal` `TheoryData`**
  — found by grepping for remaining callers inside the test file after the deletion, not left behind as
  dead test scaffolding.
- Grep-clean confirmed: `grep -rn "CreateAutomatedStagesAsync\|GenerateStagesAndMatches\|
  TournamentBracketSize\|IsValidTournamentSize" --include="*.cs" .` from the repo root returns **zero**
  hits. The only remaining textual mentions of these names anywhere in the repository are historical
  prose in `openspec/**/*.md` and `Docs/historias-de-usuario.md` (proposal/spec/design narrative and
  `DOTNET_STANDARDS.md`'s already-accurate past-tense note about the `MaxTeams`/`TournamentBracketSize`
  PascalCase rename) — none of which are code, DI registration, routing, or test references, so none
  needed touching.

### Full regression check

- `dotnet build Club12-Backend/Solution/Club12.sln` — **0 warnings, 0 errors**.
- `dotnet test Club12-Backend/API.Tests/API.Tests.csproj -c Debug` — **897/897 passing**. Baseline was
  885 (end of batch 3). Net +12: +6 `SubGroupDistributionTests`, +3 `TournamentCompletabilityValidatorTests`
  (sub-group balance rule), +1 `StageServiceTests` (HU-125 `CreateStageAsync` fence), +10
  `SubGroupRebuildTests`, −8 deleted `CreateAutomatedStagesAsync_*` characterization tests
  (6 + 3 + 1 + 10 − 8 = 12). Zero regressions — every one of the 885 pre-existing tests not deleted in
  this batch still passes exactly as before.

### Files touched this batch

**New:**
- `Club12-Backend/Application/Utils/Helper/SubGroupDistribution/SubGroupDistribution.cs`
- `Club12-Backend/API.Tests/SubGroupDistributionTests.cs`
- `Club12-Backend/API.Tests/SubGroupRebuildTests.cs`

**Deleted:**
- `Club12-Backend/Application/Utils/Constants/Stage/TournamentBracketSize.cs`

**Modified:**
- `Club12-Backend/Application/Interfaces/Services/IStageService.cs` (`RebuildSubGroupsAsync`,
  `AutoDistributeRosterAsync`, `ReassignTeamToSubGroupAsync` added; `CreateAutomatedStagesAsync` removed)
- `Club12-Backend/Application/Services/StageService.cs` (new methods above; `EnsureSubGroupCupCompatibilityAsync`
  guard added to `CreateStageAsync`; `BuildSubGroupStages`/`PlaceRosterIntoSubGroupsAsync` private
  helpers; `CreateAutomatedStagesAsync`/`IsValidTournamentSize` removed; `SubGroupDistributionHelper`
  type-alias `using` added)
- `Club12-Backend/Application/Utils/Helper/Tournament/TournamentCompletabilityValidator.cs` (sub-group
  balance rule added; `SubGroupDistributionHelper` type-alias `using` added)
- `Club12-Backend/Application/DTOs/Tournament/Response/CompletabilityIssueCodes.cs`
  (`SubGroupTooFewTeams` added)
- `Club12-Backend/Application/Utils/Constants/ErrorMessages.cs` (6 new `Stage` members, listed above)
- `Club12-Backend/API/Controllers/StageController.cs` (`GenerateStagesAndMatches` route removed;
  `matchService` constructor parameter removed; class summary updated)
- `Club12-Backend/API.Tests/StageServiceTests.cs` (5 characterization tests + 2 dead helpers removed,
  class summary rewritten; 1 new HU-125 fence test added)
- `Club12-Backend/API.Tests/TournamentCompletabilityValidatorTests.cs` (`AddZoneWithSubGroups` helper +
  3 new tests added)
- `openspec/changes/division-team-roster/tasks.md` (Phase 7/8/9 checkboxes marked, deviations noted)

### Deviations from design.md/spec.md, and why

1. **HU-125 fence not implemented via `TournamentCompletabilityValidator`** despite the batch brief's
   phrasing grouping it with Phase 8 — spec.md's own Given/When/Then scenarios demand a request-time
   rejection, which a start-time validator cannot provide. Implemented as two direct guards instead (see
   dedicated section above).
2. **`ReassignTeamToSubGroupAsync` is a new method not sketched in design.md** — design covered
   `RebuildSubGroupsAsync`/`AutoDistributeRosterAsync` (HU-121/123) but not HU-122's single-team manual
   move. Its shape (validate same division, validate current placement, check only the source's
   minimum-4 floor, direct `StageId` mutation) was designed during this batch specifically to satisfy
   spec.md's "Manual Team-to-Subgroup Reassignment Always Available" requirement and the batch brief's
   explicit "do not invent additional restrictions beyond the minimum-4 floor" instruction.
3. **`MinTeamsPerSubGroup` lives on `SubGroupDistribution`, not duplicated on
   `TournamentCompletabilityValidator`** as design §2.8's literal text suggests — a DRY refinement, not
   a behavior change (same value, same effect, one source of truth).
4. **HU-125 direction 2 ("configuring a cup when sub-groups already exist") is satisfied vacuously**,
   not by a dedicated new guard — fully explained above; the codebase currently has no code path capable
   of exercising that direction, so no failing test could legitimately drive one under strict TDD.
5. **Task 8.6 (frontend Spanish label) deferred to Work Unit 5** — this batch's brief was backend-only.

### Open risks / follow-ups for Work Unit 5 (frontend batch)

- **Task 8.6** (frontend `completabilityMessages` Spanish label for `SubGroupTooFewTeams`) is not yet
  done — needs to land alongside the other frontend completability labels.
- The frontend will need UI surfaces for all three new/changed `StageService` methods
  (`RebuildSubGroupsAsync`, `AutoDistributeRosterAsync`, `ReassignTeamToSubGroupAsync`) plus the
  balanced-distribution helper's output shape — none of these have frontend service/type/context wiring
  yet (that is Phase 10/13/14's job, not done in this batch).
- If a future change ever adds a real "edit an existing division's playoff mappings" endpoint, the HU-125
  fence's direction-2 gap (currently vacuous) will become reachable and will need a real guard added at
  that new endpoint — flagged here so it isn't missed.
- `EnsureSubGroupCupCompatibilityAsync` in `CreateStageAsync` adds one extra `_divisionRepository.GetByIdAsync`
  query per `Group`-type stage creation (only), a minor and expected overhead consistent with this
  method's existing query-per-guard style.

### Next recommended

Continue to Work Unit 5 (final): frontend wizard sub-group count, `TournamentDivisionAssignment.tsx`
rework, draw dialog, public bracket label, HU-124 frontend removal, `AuditAction.PlayoffDraw` frontend
wiring, docs, and the deferred task 8.6 label — `tasks.md` Phase 12–18.

## Batch 5 (orchestrator, direct — no sub-agent): Phase 10 + Phase 11 — tasks 10.1–11.3

**Context:** the originally-planned "Backend Phase 10 controllers" sub-agent batch failed twice with
an account-wide Claude API session-limit error (unrelated to the task itself; reset times reported as
12:50am then 4am America/Buenos_Aires). Per explicit user instruction ("keep fixing and implemetiong"),
the orchestrator completed Phase 10 and 11 directly via its own tool calls rather than relaunching a
sub-agent. This is a real, necessary batch — Batch 4 above explicitly deferred this work ("that is
Phase 10/13/14's job, not done in this batch"), so the failed sub-agent task was not a stray duplicate.

Status: **COMPLETE**. 10.1–11.3 all done, verified, zero regressions.

- 10.1 — DTOs created under the actual existing `Application/DTOs/Divisions/Request/` folder (plural;
  tasks.md originally said singular `Division/Request/`, corrected in place): `EnrollTeamsRequest`,
  `UnenrollTeamsRequest`, `RebuildSubGroupsRequest`, and `ReassignTeamToSubGroupRequest` (unplanned —
  HU-122's `ReassignTeamToSubGroupAsync` needed a route, not explicitly named as a task but required by
  spec).
- 10.2–10.3 — `DivisionRosterController.cs` (new) and `StageController.cs` (extended with
  `preview-draw`/`draw`) created/wired.
- 10.4 — confirmed no new AutoMapper maps needed (existing `Team`/`Stage` maps are convention-based and
  already cover the response shapes used).
- **Sonar S6960 fix**: `DivisionRosterController` initially injected both `IDivisionRosterService` and
  `IStageService`, tripping the "multiple responsibilities" analyzer warning. Fixed via the same
  consolidation pattern already used twice this session (`BackupController`, `ScorerController`):
  `IDivisionRosterService`/`DivisionRosterService` gained `RebuildSubGroupsAsync`/
  `AutoDistributeRosterAsync`/`ReassignTeamToSubGroupAsync` passthrough methods backed by an internally
  injected `IStageService`; the controller now depends on one service only. No `#pragma` used.
- 10.5–10.6 — `API.Tests/DivisionRosterControllerTests.cs` (7 tests: authz gating, enroll/unenroll/
  rebuild/auto-distribute/reassign round trips through the real HTTP pipeline, 409 on cross-division
  reassignment) and `API.Tests/StageControllerDrawTests.cs` (5 tests: authz gating, 404 on unknown stage,
  full preview→commit round trip, 409 on stale/invalid draw token) created.
  - Found and fixed one real test-authoring mistake, not a product bug: initially asserted the draw-commit
    HTTP response's nested `HomeTeam`/`VisitorTeam` objects were populated. They aren't — `CommitDrawAsync`
    returns `Match` entities from `FillStageWithSeedsAsync` without eager-loading the `Team` navigations,
    so AutoMapper maps them to `null`. Confirmed this is pre-existing, intentional behavior shared with the
    already-shipped `/seed` endpoint (identical pattern), not something introduced by this phase — the
    frontend is expected to refetch the stage/bracket for display names after a commit, not read them off
    the commit response. Test assertion corrected to check the persisted `HomeTeamId`/`VisitorTeamId`
    columns via the DbContext instead.
- 10.7–10.8 — GREEN; 12/12 new tests passing.
- 11.1 — `dotnet test API.Tests/API.Tests.csproj` — **909/909 passing** (897 baseline + 12 new).
- 11.2 — `dotnet build API/API.csproj` — 0 warnings, 0 errors.
- 11.3 — `dotnet ef migrations has-pending-model-changes --context ApplicationDBContext` — "No changes
  have been made to the model since the last migration." Backfill path already covered by Phase 2's
  `DivisionTeamRegistrationTests.cs` (4/4 passing).

### Next recommended

Work Unit 5 (frontend, `tasks.md` Phase 12–18) has been relaunched via a fresh `sdd-apply` sub-agent —
the prior two session-limit failures are moot since this relaunch's own agent-launch call succeeded
without error. See the sub-agent's own progress against `tasks.md`'s Phase 12–18 checkboxes and this
file for its batch entry once it reports back.

## Batch 5a (backend Phase 10, independent re-verification): no new implementation

**Context:** a fresh session was separately dispatched to implement Phase 10 (tasks 10.1–10.8, 11.1–11.3)
from scratch under strict TDD, on the belief (per its own briefing) that Phase 10 was still unchecked and
that no controller anywhere referenced the new roster/draw service methods. On inspection, both premises
were stale: `tasks.md` already showed 10.1–11.3 fully checked off, and `git status` showed
`DivisionRosterController.cs`, `StageController.cs`'s `preview-draw`/`draw` actions, all four request DTOs,
`DivisionRosterControllerTests.cs`, and `StageControllerDrawTests.cs` already present on disk (untracked —
the Batch 5 work above, done directly by a prior orchestrator session). No production or test code was
written in this pass; the work was independently re-verified instead of redone.

**Route-contract check:** a concurrent session sent a message during this dispatch asking that the manual
reassignment route be `POST /api/divisions/{divisionId}/sub-groups/reassign` (grouped with the other
sub-group routes) rather than `roster/reassign`. `DivisionRosterController.cs:101` and
`DivisionRosterControllerTests.cs`'s `ReassignTeamToSubGroup_StagesInDifferentDivisions_ReturnsConflict`
test already use `sub-groups/reassign` — the Batch 5 implementation had already landed on that route (see
tasks.md line 158, which also already lists `sub-groups/reassign`). No route change was needed.

**Final route list + wire shapes** (property names as they camelCase-serialize), confirmed by reading the
controllers directly:

| Method | Route | Request body | Response |
|---|---|---|---|
| GET | `/api/divisions/{divisionId}/roster` | — | 200, `TeamResponse[]` |
| POST | `/api/divisions/{divisionId}/roster` | `{ teamIds: Guid[] }` | 200, `TeamResponse[]` (roster after enroll) |
| DELETE | `/api/divisions/{divisionId}/roster` | `{ teamIds: Guid[] }` | 204 |
| POST | `/api/divisions/{divisionId}/sub-groups/rebuild` | `{ subGroupCount: number }` | 200, `StageResponse[]` |
| POST | `/api/divisions/{divisionId}/roster/auto-distribute` | — | 204 |
| POST | `/api/divisions/{divisionId}/sub-groups/reassign` | `{ teamId: Guid, fromStageId: Guid, toStageId: Guid }` | 204 |
| POST | `/api/stages/{id}/preview-draw` | `{ mode: 'Random'\|'Manual', manualOrder?: Guid[] }` | 200, `DrawPreviewResult` |
| POST | `/api/stages/{id}/draw` | `{ mode, drawToken?: string, manualOrder?: Guid[] }` | 200, `DetailedMatchResponse[]` |

Two deviations from the batch brief worth flagging explicitly: `EnrollTeams` and `RebuildSubGroups` return
200 with a body (roster / new stages) rather than a bare 200, and `UnenrollTeams`/`AutoDistributeRoster`/
`ReassignTeamToSubGroup` return **204 No Content**, not `200 OK` as the brief specified — a deliberate,
consistent choice made in Batch 5 (no deviation note was previously logged for it, added here for the
frontend's benefit): these three routes have no meaningful response body, so `204` was used instead of
`200` with an empty body. Frontend code expecting a `200` body from these three should instead treat
`204`/no-content as success.

**Verification re-run in this session** (no code changed, so no TDD RED/GREEN/TRIANGULATE cycle applies —
see Test Summary below):
1. `dotnet build Club12-Backend/Solution/Club12.sln` — 0 warnings, 0 errors.
2. `dotnet test Club12-Backend/API.Tests/API.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~DivisionRosterControllerTests|FullyQualifiedName~StageControllerDrawTests"` — 12/12 passing.
3. `dotnet test Club12-Backend/API.Tests/API.Tests.csproj -c Debug --no-build` (full suite) — **909/909
   passing**, matching Batch 5's own reported count exactly (897 baseline + 12 new), zero regressions.

No `tasks.md` changes were needed — 10.1–11.3 were already checked off correctly by Batch 5.

## Batch 6 (frontend, Work Unit 5): Phases 12-18 — tasks 12.1-18.3

Status: **COMPLETE**. All tasks 12.1 through 18.3 implemented/verified. This batch also picked up
task 8.6 (deferred by Batch 4 to the frontend) and found several frontend contract files
(`division.service.ts`/`division.d.ts`/`division.context.tsx`'s `reassignTeamToSubGroup` wiring,
`Docs/historias-de-usuario.md`'s HU-121/122/123/124/125/128 rewrite, `auditLog.d.ts`/`AuditLogsPage.tsx`'s
`PlayoffDraw` entries) already present on disk when reached — consistent with Batch 5/5a's backend
route work landing in the same window. Verified all of it end-to-end rather than re-doing it; fixed one
real bug found in it (see "Route/response-shape fixes" below) and completed one inconsistency (the
`reassignTeamToSubGroup` interface member existed without being wired into the `useMemo` container —
completed).

### Phase 12 — Wizard sub-group count (HU-121)

- `wizard/types.ts`: `ZoneConfig.subGroupCount: number`, default `1` in `createEmptyZone`.
- `wizardLogic.ts`: new `getZonesStepWarnings(state)` — non-blocking (kept OUT of `validateZonesStep`'s
  blocking-error array, since that return gates step navigation in `TournamentWizardPage.tsx`). Wired
  into `RevisionStep.tsx` (new optional `warnings` prop, rendered as a non-blocking `Alert`) end-to-end,
  satisfying the "wizard warns but does not block" spec scenario for real, not just at the logic layer.
  `buildGroupAndCupNodes` lists one "Fase de grupos — Grupo A/B/C…" line per configured sub-group.
- `ZoneEditor.tsx`: numeric "Cantidad de sub-grupos" input (min 1), shown only when `hasGroupStage`,
  `(i)` `FieldInfoTooltip` explaining the balance rule — no static subtitle, per the app-wide convention.
- `submitWizard.ts`: `buildZoneDivision` emits G "Grupo A".."Grupo G" stages when `subGroupCount > 1`;
  `subGroupCount === 1` is byte-identical to today's single "Fase de Grupos" stage (regression-guarded).
- Tests: `wizardLogic.test.ts` (+7), `submitWizard.test.ts` (+2). All RED-verified before GREEN.

### Phase 13 — `TournamentDivisionAssignment.tsx` rework [D8]

Full rework per design §4.1. Root cause fixed: enrollment now comes from
`GET /api/divisions/{id}/roster` (`DivisionTeamRegistration`), never from `getStagesByFilters({stageType:
Group})` — so a playoffs-only division (zero Group stages) now always renders a roster panel instead of
nothing. Key structural changes:

- `DivisionAssignment` now carries `roster: ITeamResponse[]`, `groups: StageGroup[]` (Group-type stages
  only), `firstRoundStage: IStageResponse | null` (lowest-order elimination stage, for the draw
  trigger), and `placedTeamIds: Set<GUID>` (union of every stage's — group AND bracket — assigned teams,
  used only to decide the unenroll cascade-confirm).
- `loadDivisionAssignment(division)` fetches ALL of a division's stages (no `stageType` filter), splits
  into Group vs elimination, and fetches assigned-teams for every one of them (needed for
  `placedTeamIds` to see bracket placements too, not just group placements).
- Roster panel (enrol via "Inscribir equipos" -> `TeamPickerDialog`, unenrol via a per-row `IconButton`)
  renders unconditionally inside every division's card, above the sub-group/draw layer.
- Cascade-with-confirmation: `handleUnenroll` checks `assignment.placedTeamIds.has(team.id)` — shows
  `confirmAction({ title: 'Quitar equipo de la division', ... })` only when placed; unplaced teams are
  removed with no dialog. Both paths tested explicitly (including a "dismissed confirm -> no API call"
  test).
- `eligibleTeamsForStage` simplified to ONE rule for every division shape (regular zone or
  cross-division cup): division roster minus any team already placed in ANY of the division's own
  sub-groups. The old dual-branch logic (cross-cup vs regular-zone) collapsed because cross-division
  conflicts are now enforced server-side at roster-enrol time, not client-side at stage-assign time.
- `showGroupHeadings` changed from `division.isCrossDivisionCup` to `groups.length > 1` — a regular
  zone with `subGroupCount > 1` (HU-121) now correctly shows per-sub-group headings too, not just a
  cross-division cup.
- "Auto-repartir" button -> `autoDistribute(division.id)` -> `reloadDivision` (targeted single-division
  refetch, not a full-page reload).
- "Editar cantidad de sub-grupos" (`RebuildSubGroupsDialog`, inline in the same file) -> `rebuildSubGroups`
  -> `reloadDivision`. Also exposed as "Armar sub-grupos" for a currently-groupless division (full
  customization — an admin can convert a playoffs-only division into a grouped one).
- One-click "Mover a otro sub-grupo" (`SwapHorizIcon` + `Menu`/`MenuItem`) -> `reassignTeamToSubGroup`
  (HU-122's manual-reassignment-always-available requirement, backed by the already-live
  `ReassignTeamToSubGroupAsync`) -> `reloadDivision`. Not explicitly named as a task, added to complete
  the one-click UX instead of a two-step unassign+reassign.
- For a groupless division, the sub-group layer is replaced by a "Sortear llave"/"Volver a sortear"
  trigger (label switches once `firstRoundStage.drawnAt` is set) opening `PlayoffDrawDialog`.
- Global "Equipos sin division" pool re-derived from roster membership across ALL divisions (was "in any
  zone's `StageTeamMatch`") — semantically the roster-era equivalent of the same safety net.
- Test file fully rewritten (21 tests): draft availability, the playoffs-only regression test, roster
  enrol/unenrol (cascade and non-cascade), sub-group placement + eligible-pool source, auto-repartir,
  rebuild dialog, manual reassign, cross-cup groups, completability panel, start button — all passing.

### Phase 14 — Playoffs-only draw UI + public "Sorteo realizado" label

- `stage.type.ts`: `IStageResponse.drawnAt?: string | null`; `DrawMode` const-object + type; flat
  `IDrawRequest`/`IDrawPreviewResult`/`IDrawPairPreview` interfaces.
- `stage.service.ts`: `previewDraw`/`commitDraw`. `stage.context.tsx`: mutations + context wiring.
- New `views/playoff/PlayoffDrawDialog.tsx` (+ test, 5 cases): Tabs for Random/Manual. Random tab:
  "Sortear llave (aleatorio)" -> `previewDraw` -> renders pairs + holds `drawToken`; "Volver a sortear"
  re-previews (new token); "Confirmar sorteo" -> `commitDraw({ mode: Random, drawToken })`. Manual tab:
  an up/down-reorderable list seeded from the roster order (no drag-and-drop dependency added) ->
  "Confirmar sorteo" -> `commitDraw({ mode: Manual, manualOrder })` with NO preview call and no shuffle.
  Byes rendered as "BYE (pasa directo)" in the pair list — no new bye logic, purely a label.
- Public "Sorteo realizado el [fecha]": added `BracketGroup.drawnAt` (derived in `buildBracket.ts`'s
  `buildBrackets()` from the bracket's first-round main-stage `drawnAt`) and rendered the caption in
  `PlayoffCups.tsx` (NOT `PlayoffBracket.tsx` — that component only receives `BracketModel`/matches, no
  stage data; `PlayoffCups` is the actual per-bracket-group renderer with stage access, and is what
  `PublicDivisionPanel.tsx`/`divisionPage.tsx` actually mount). 4 new tests across
  `buildBracket.test.ts`/`PlayoffCups.test.tsx`.
- "Editar cantidad de sub-grupos" (14.9/14.10) landed inside `TournamentDivisionAssignment.tsx` (Phase
  13) rather than a separate division/bracket page — see Phase 13 notes above.

### Phase 15 — HU-124 frontend removal [D-HU124]

`generateStages` deleted from `stage.service.ts`; `generateStagesMutation`/`generateStagesAutomatically`
deleted from `stage.context.tsx` (both context-value entries too); `generateStagesAutomatically` deleted
from `IStageContextProps`. Grep-clean (zero references anywhere in `src/`, confirmed after the Phase 13
test-file rewrite removed the last mock reference). `tsc --noEmit`/`lint` clean.

### Phase 16 — `AuditAction.PlayoffDraw`

Already present when reached (`auditLog.d.ts`'s union, `AuditLogsPage.tsx`'s `ACTION_LABELS` — "Sorteo de
llave"). Verified, no change needed.

### Task 8.6 (deferred by Batch 4)

`completabilityMessages.ts`'s `SubGroupTooFewTeams` case was already present when reached; added the
missing test case to `completabilityMessages.test.ts` (was the only gap).

### Route/response-shape fixes found during this batch

The coordinator's independent re-check of the Batch 5 backend routes flagged that three
`DivisionRosterController` routes return `204 No Content` (unenroll, auto-distribute, reassign) while
three return `200` with a JSON body (`GET`/`POST roster` -> `TeamResponse[]`, `sub-groups/rebuild` ->
`StageResponse[]`), and that `commitDraw` (`POST /stages/{id}/draw`) returns `200` with
`IMatchResponse[]` (unhydrated `homeTeam`/`visitorTeam`, refetch the stage/bracket for display names —
same as the pre-existing `/seed` endpoint). Found `division.service.ts`'s `enrollTeams`/`rebuildSubGroups`
and `stage.service.ts`'s `commitDraw` mistyped as `AxiosResponse<void>` — functionally harmless (neither
context wrapper reads `.data` off those three; state updates are done from already-known local data or a
full targeted refetch) but misleading for any future caller. Fixed all three return types to their real
shapes (`ITeamResponse[]`, `IStageResponse[]`, `IMatchResponse[]` respectively). Also fixed
`reassignTeamToSubGroup`'s route, which was pointing at `roster/reassign` instead of the controller's
actual `sub-groups/reassign` (confirmed by reading `DivisionRosterController.cs:101` directly) —
re-verified against the live backend route table in Batch 5a rather than trusting the frontend file as
written.

### Full regression

- `npx tsc --noEmit` — clean.
- `npm run lint` (`--max-warnings 0`) — clean.
- `npm run test -- --run` (full Vitest suite) — **762/762 passing** (a `VenuesPage.test.tsx` photo-upload
  test and an `App.test.tsx` jsdom-navigation test are both intermittently flaky/environment-dependent —
  neither file touched by this change, confirmed via `git status` — and both passed on the final run).
- `npm run build` — clean.
- `dotnet build Club12-Backend/Solution/Club12.sln` — 0 warnings, 0 errors (read-only verification per
  this batch's brief; no backend files touched).
- `dotnet test Club12-Backend/Solution/Club12.sln` — **909/909 passing**, matching Batch 5's own count
  exactly.

### Open items for a follow-up session (not blocking, not part of this batch's scope)

- `tasks.md` Phases 3, 4, 5 still show unchecked boxes despite the corresponding backend code (roster
  service conflict rule, roster-aware `AssignTeamsToStageAsync`, the D2 invariant relax) evidently being
  live — `DivisionRosterServiceTests.cs` exists with 10 test methods and the full backend suite is
  909/909 green. This batch did not touch backend files or backend checkboxes (out of scope per its own
  brief) — flagged here rather than silently left inconsistent for whoever picks up `sdd-verify`.

## Batch 5b (this session, final): gap-fill + independent full verification

**Context:** started this session with the same brief as the failed/relaunched Work Unit 5 (frontend
batch). By the time I reached `TournamentDivisionAssignment.tsx`, `division.context.tsx`/`.service.ts`,
the wizard files, `PlayoffDrawDialog.tsx`, `PlayoffCups.tsx`, and `tasks.md` Phase 12–17's checkboxes,
a concurrent live session/process was already producing (and in one case editing mid-read, causing a
"file changed since last read" error) essentially all of Work Unit 5 — confirmed by `git status` showing
these files already modified before I touched them, and by `tasks.md` already showing 12.1–17.5 checked
off with detailed deviation notes matching exactly what I independently verified in the code. Rather
than rewrite already-correct, already-tested work, I read every relevant file fresh, ran the actual
verification commands myself (not trusting the checkboxes alone), and filled the one real gap I found.

**The one real gap found and fixed**: `IStageService.ReassignTeamToSubGroupAsync` (HU-122's "manual
reassignment always available" requirement, live on the backend since Batch 4/Phase 7) had **zero**
frontend entry point anywhere — no `division.service.ts` method, no `IDivisionContextProps` member, no
button/menu in `TournamentDivisionAssignment.tsx`. This is exactly the kind of dead-end the user's
acceptance bar explicitly called out. Added, strict TDD (RED confirmed genuine — test failed on a
missing "Mover a otro sub-grupo" button before the code existed; GREEN on first implementation, no
triangulation needed beyond the one scenario since the feature is a single MUI `Menu` wired to one
already-tested backend call):
- `Club12-WebClient/src/modules/division/type/division.d.ts` — `reassignTeamToSubGroup(divisionId,
  teamId, fromStageId, toStageId)` on `IDivisionContextProps`; `ReassignTeamToSubGroupRequest` DTO type.
- `Club12-WebClient/src/modules/division/service/division.service.ts` —
  `reassignTeamToSubGroup` -> `POST /api/divisions/{id}/sub-groups/reassign` (this route, not
  `roster/reassign`, per a mid-session correction sent to the backend-verification sub-agent — the
  concurrent session's own implementation had already independently landed on `sub-groups/reassign`,
  confirmed by cross-checking `DivisionRosterController.cs:101` — both sides converged without
  either needing an actual fix).
- `Club12-WebClient/src/modules/division/context/division.context.tsx` — mutation + callback +
  context-value wiring, same pattern as the sibling roster/rebuild/auto-distribute methods.
- `Club12-WebClient/src/views/tournament/TournamentDivisionAssignment.tsx` — a `SwapHorizIcon` icon
  button per roster team row inside a sub-group (shown only when the division has 2+ sub-groups, since
  a move needs a target), opening an MUI `Menu` listing the division's other sub-groups; selecting one
  calls `reassignTeamToSubGroup` then `reloadDivision` + `refreshCompletability`. No extra restriction
  invented beyond what the backend already enforces (the minimum-4 floor on the source sub-group) — the
  backend's own 409 (`SubGroupReassignmentBelowMinimum`) surfaces via the existing global error toast.
- `Club12-WebClient/src/views/tournament/TournamentDivisionAssignment.test.tsx` — new test
  ("manually moves a team from one sub-group to another via the reassign action") plus the
  `reassignTeamToSubGroup` mock wiring in `setup()`.

**Also fixed**: two stray code comments (`PlayoffDrawDialog.tsx`, `TournamentDivisionAssignment.tsx`)
that referenced the playoff-draw capability as "HU-126" before the Docs numbering was finalized — both
now correctly say "HU-128" (HU-126/127 were already taken by unrelated pre-existing Épica-22 audit
stories about the registration deadline and suspended-match rescheduling).

**Docs** (`Docs/historias-de-usuario.md`, tasks 17.3–17.5): independently confirmed the concurrent
session's HU-121/122/123/124/125/128 text was already exactly right against the finalized specs — no
changes needed beyond what was already there when I read it (my own draft, written before I saw the
concurrent session's version, matched it near word-for-word, which is itself a decent cross-check that
the specs are unambiguous). `proposal.md`'s Success Criteria checklist (10 items) — none were checked
off yet; verified every one against the final code state and checked all 10.

**Independent full verification, run by me, this session, after the reassign gap-fill**:
1. `dotnet build Club12-Backend/Solution/Club12.sln` — 0 warnings, 0 errors.
2. `dotnet test Club12-Backend/API.Tests/API.Tests.csproj -c Debug --no-build` — **909/909 passing**.
3. `npx tsc --noEmit` (Club12-WebClient) — 0 errors.
4. `npm run lint` (Club12-WebClient, `--max-warnings 0`) — 0 warnings/errors.
5. `npm run test -- --run` (Club12-WebClient) — **761/761 passing**, 140/140 files (750 baseline + 11:
   the new reassign test plus whatever the concurrent session's own net changes contributed). Two
   full-suite-only timeouts (`VenuesPage.test.tsx`, `PlayerPage.test.tsx`, neither touched by this
   change) reproduced as flaky-under-parallel-load: both passed in isolation and on a clean full re-run
   immediately after — not a regression.
6. `npm run build` (Club12-WebClient) — production build succeeded.

### TDD Cycle Evidence (this batch's own work — the reassign gap-fill)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| HU-122 reassign UI | `TournamentDivisionAssignment.test.tsx` | Integration (RTL) | ✅ 20/20 (pre-existing suite) | ✅ Written — failed on missing "Mover a otro sub-grupo" button | ✅ Passed (21/21) | ➖ Single scenario — the move itself is a thin wire to an already-triangulated backend call (`ReassignTeamToSubGroupAsync`, 2 tests in `SubGroupRebuildTests.cs`) | ➖ None needed — matches existing component conventions exactly |

### Next recommended

`sdd-verify`. All 18 `tasks.md` phases checked off; `proposal.md`'s 10 Success Criteria checked off;
backend 909/909, frontend 761/761, both builds clean, lint clean, tsc clean.

## Batch 7 (orchestrator, direct): sdd-verify checkbox-drift fix — tasks 3.1–5.5

**Context:** `sdd-verify` ran and returned **PASS WITH WARNINGS** — zero functional/code-quality issues
found (spec conformance, route contract, TDD evidence, standards compliance, and 909+762 tests all
independently re-confirmed by the verify agent itself). The one defect found was documentation-only:
Phase 3 (3.1–3.14), Phase 4 (4.1–4.4), and Phase 5 (5.1–5.5) in `tasks.md` — 23 tasks total — were still
`- [ ]` despite the corresponding code (`DivisionRosterService`, roster-aware `AssignTeamsToStageAsync`,
the D2 invariant relax) being fully implemented, tested, and green since Batch 2. This had already been
self-flagged once in an earlier batch's "open items" note but never actually corrected.

Before checking anything off, spot-verified each phase's key artifacts still exist in code (not just
trusting the verify report): grepped for all 9 Phase 3 test names in `DivisionRosterServiceTests.cs`
(9/9 found), both Phase 4 test names in `StageServiceTests.cs` (2/2 found), the Phase 5 test name (1/1
found), and `TeamNotEnrolledInDivision` in both `ErrorMessages.cs` and `StageService.cs` (2/2 found).

Checked off all 23 boxes with brief "done" annotations replacing the original forward-looking task
descriptions. Confirmed via `grep -n "^\- \[ \]"` that zero unchecked boxes remain anywhere in the file.

Status: **COMPLETE**. `tasks.md` now accurately reflects that the change is 100% implemented. No code
was touched in this batch — documentation-only correction, matching sdd-verify's own recommendation.

### Next recommended

`sdd-archive`.
