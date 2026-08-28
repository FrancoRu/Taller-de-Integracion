# Tasks: Season-Scope a Team's Tournament Participation (HU-98)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~950–1250 total (backend core ~250; migration + EF snapshot ~150–350; tests ~350–450; frontend ~0–20 regression-only) |
| 800-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (foundation + migration/backfill) → PR 2 (upsert/filter rewrite + tests) → PR 3 (frontend regression) |
| Delivery strategy | single-pr |
| Chain strategy | size-exception |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: size-exception
800-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Domain/Infra foundation + migration + idempotent backfill (additive only, no behavior change) | PR 1 | `dotnet test Club12-Backend/Solution/Club12.sln --filter TeamTournamentRegistrationTests.Backfill_JoinLogic` | Dry-run both backfill SELECTs against a prod snapshot | `Down()` drops the table; `Team.TournamentId` and all reads stay intact |
| 2 | `RegisterTeamsToTournamentAsync` upsert + `GetAllTeamsAsync` join filter + seed + full test suite | PR 2 | `dotnet test Club12-Backend/Solution/Club12.sln --filter TeamTournamentRegistrationTests` | `dotnet test Club12-Backend/Solution/Club12.sln` (full suite) | Revert `TeamService.cs`/`QueryableExtensions.cs`/`SampleTournamentBuilder.cs`; PR 1's table stays, app keeps functioning |
| 3 | Frontend regression verification (no functional change expected) | PR 3 | `npm run test --prefix Club12-WebClient` | Existing e2e wizard specs | N/A — no production frontend code touched unless a break is found, then isolated to that one file |

## Phase 1: Backend Domain & Infrastructure Foundation

- [ ] 1.1 Create `Club12-Backend/Domain/Entities/Models/TeamTournamentRegistration.cs` (`TeamId`, `Team?`, `TournamentId`, `Tournament?`, mirror `PlayerTeamRegistration` XML docs)
- [ ] 1.2 Modify `Club12-Backend/Domain/Entities/Models/Team.cs` — add `ICollection<TeamTournamentRegistration> TeamTournamentRegistrations` nav
- [ ] 1.3 Create `Club12-Backend/Application/Interfaces/Repositories/ITeamTournamentRegistrationRepository.cs : IGenericRepository<TeamTournamentRegistration>`
- [ ] 1.4 Modify `Club12-Backend/Application/Interfaces/Repositories/IUnitOfWork.cs` — add `TeamTournamentRegistrationRepository` property
- [ ] 1.5 Create `Club12-Backend/Infrastructure/Repositories/TeamTournamentRegistrationRepository.cs : GenericRepository<TeamTournamentRegistration>`
- [ ] 1.6 Modify `Club12-Backend/Infrastructure/Repositories/UnitOfWork.cs` — ctor param + property wiring
- [ ] 1.7 Modify `Club12-Backend/Infrastructure/Persistance/ApplicationDBContext.cs` — add `DbSet<TeamTournamentRegistration>`
- [ ] 1.8 Modify `Club12-Backend/Infrastructure/Persistance/EntityConstants.cs` — add `TeamTournamentRegistration = "TeamTournamentRegistrations"`
- [ ] 1.9 Create `Club12-Backend/Infrastructure/Persistance/Configurations/TeamTournamentRegistrationEntityConfiguration.cs` — unique index `(TeamId, TournamentId)`, cascade FKs, `ToTable(EntityConstants.Tables.TeamTournamentRegistration, Schema)`

## Phase 2: Migration & Backfill (backend)

- [ ] 2.1 RED — add `Backfill_JoinLogic` test to new `Club12-Backend/API.Tests/TeamTournamentRegistrationTests.cs` asserting expected (TeamId, TournamentId) pairs via `StageTeamMatch → Stage → Division` LINQ (mirror the player backfill test)
- [ ] 2.2 Run `dotnet ef migrations add AddTeamTournamentRegistrationTable` in `Club12-Backend/Infrastructure/Migrations/` — table, unique index, FKs
- [ ] 2.3 Add two-step idempotent backfill SQL to migration `Up()`: (1) `INSERT ... SELECT DISTINCT` from `StageTeamMatches → Stages → Divisions`, (2) `INSERT ... SELECT` from `Teams` where `TournamentId IS NOT NULL`, both `NOT EXISTS`-guarded against the unique index
- [ ] 2.4 Dry-run both backfill SELECTs against a prod snapshot; confirm `Division.TournamentId` consistency (open question from design)
- [ ] 2.5 GREEN — verify 2.1's test passes

## Phase 3: Application Layer — Filter Utility & Seed (backend)

- [ ] 3.1 Modify `Club12-Backend/Application/Utils/Extensions/QueryableExtensions.cs` — add `params string[] ignoredProperties` to `ConstructFilterExpression<TEntity, T>` (default empty), feed into the `ShouldSkipProperty` predicate
- [ ] 3.2 Modify `Club12-Backend/Infrastructure/Persistance/SampleTournamentBuilder.cs` — seed one `TeamTournamentRegistration` per built team (mirror the player-registration seed)

## Phase 4: RegisterTeamsToTournamentAsync Upsert (backend, TDD)

- [ ] 4.1 RED — `RegisterTeamsToTournamentAsync_NewTeam_CreatesRegistrationAndUpdatesPointer` in `TeamTournamentRegistrationTests.cs`
- [ ] 4.2 RED — `RegisterTeamsToTournamentAsync_DroppedTeam_RemovesOnlyTargetTournamentRegistration_KeepsOthers`
- [ ] 4.3 RED — `RegisterTeamsToTournamentAsync_EmptyList_ClearsOnlyTargetTournamentMembers_KeepsOtherTournaments`
- [ ] 4.4 RED — `RegisterTeamsToTournamentAsync_ExistingMember_StaysRegistered_NotDuplicated`
- [ ] 4.5 RED — `RegisterTeamsToTournamentAsync_TeamInDifferentTournament_GainsSecondRegistration_KeepsFirst`
- [ ] 4.6 GREEN — rewrite `RegisterTeamsToTournamentAsync` in `Club12-Backend/Application/Services/TeamService.cs` per the design's upsert data flow (remove-scoped / add-new / refresh-or-clear pointer / bulk save)
- [ ] 4.7 Verify 4.1–4.5 pass: `dotnet test Club12-Backend/Solution/Club12.sln --filter TeamTournamentRegistrationTests`

## Phase 5: GetAllTeamsAsync Join Filter (backend, TDD)

- [ ] 5.1 RED — `GetAllTeamsAsync_TournamentFilter_ResolvesViaJoin_IncludesReassignedButRegisteredTeam`
- [ ] 5.2 RED — `GetAllTeamsAsync_StageIdFilter_StillWorksAlongsideTournamentId` (combined-filter regression guard for the existing `StageId` special-case)
- [ ] 5.3 GREEN — rewrite the `TournamentId` branch in `GetAllTeamsAsync` (`TeamService.cs`): call `ConstructFilterExpression(filter, nameof(GetTeamsFilteredRequest.TournamentId))`, then `.And(t => t.TeamTournamentRegistrations.Any(r => r.TournamentId == filter.TournamentId.Value))`
- [ ] 5.4 Verify 5.1–5.2 pass

## Phase 6: Backend Full Regression

- [ ] 6.1 Run `dotnet test Club12-Backend/Solution/Club12.sln` (full suite) — confirm no `PlayerTeamRegistration`/`Team`/`Tournament` regressions
- [ ] 6.2 Confirm re-registering an existing pair does not duplicate the row (unique index holds, `Assert.Single`) — covered by 4.4

## Phase 7: Frontend Verification (no functional change expected)

- [ ] 7.1 Confirm `submitWizard.ts` set-replacement contract still matches the within-tournament-only upsert semantics — read-only check, no edit expected
- [ ] 7.2 Confirm `ITeamResponse.tournamentId`, `EquiposStep`, `TeamRegisterPage` still compile/behave against the unchanged `Team` DTO shape
- [ ] 7.3 Run `npm run test --prefix Club12-WebClient` (Vitest) — regression only
- [ ] 7.4 Run existing e2e wizard specs — regression only; touch frontend code only if a genuine behavior break is found
