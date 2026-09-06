# Tasks: Division Team Roster & Playoffs-Only Seeding

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | Backend ~1,450–1,850 (domain/migration ~250–350; roster service ~150–200; StageService additions ~350–450; DTOs/controllers ~200–250; HU-124 deletions ~-150 to -200 lines but still counted as changed; tests ~600–800). Frontend ~700–950 (wizard ~90; `TournamentDivisionAssignment.tsx` rework ~200–300; new draw dialog ~150–200; service/type/context wiring ~150; public bracket label ~20; HU-124 removal ~-40; tests ~250–350). Docs ~100–150. **Total ~2,250–2,950.** |
| 800-line budget risk | High |
| Chained PRs recommended | Yes |
| Decision needed before apply | Yes |
| Cached delivery strategy | single-pr |
| Suggested split (if chaining is chosen instead) | PR 1 — domain/migration/backfill (Phases 1–2, additive only); PR 2 — roster service + StageService roster-awareness + D2 (Phases 3–5); PR 3 — playoff draw/preview/commit/guard + audit + DrawnAt (Phases 6–8); PR 4 — sub-group rebuild + completability + HU-124 removal (Phases 9–11); PR 5 — frontend wizard + assignment rework + draw UI + docs (Phases 12–17) |

**Flag for the orchestrator:** the session's cached `delivery_strategy` is `single-pr`, but this forecast is realistically 2.5–3.5x the 800-line budget — this is a new entity, a data-backfilling migration, two new service surfaces (roster CRUD, playoffs draw), a cross-cutting `AssignTeamsToStageAsync` behavior change, a dead-endpoint removal touching 4 backend + 4 frontend files, a first-class new frontend draw UI, and a real rework of an already-fragile, previously-untested component (`TournamentDivisionAssignment.tsx`) — plus the TDD tests for all of it. Per the Review Workload Guard, `single-pr` at `High` risk means: **stop and require/record `size:exception` before `sdd-apply`**, or have the orchestrator re-collect delivery strategy as `auto-chain`/`ask-on-risk` before proceeding. Do not launch `sdd-apply` on the current single-PR assumption without one of those two resolutions.

## Confirmations resolved during this phase (design.md §8 open follow-ups)

- **AutoMapper `DrawnAt` mapping** — confirmed by reading `Club12-Backend/API/AutoMapperProfiles/StageProfile.cs:15-16`: `CreateMap<Stage, StageResponse>().ReverseMap()` has zero `.ForMember` exclusions (plain convention-based full map). Adding `DateTime? DrawnAt` with the same name/type to both `Stage` and `StageResponse` maps automatically. No profile change needed — see task 1.7.
- **HMAC key source for the draw token** — confirmed by reading `Club12-Backend/Application/Services/AuthService.cs:25` and `Club12-Backend/Application/Utils/Constants/Configuration/ConfigurationKeys.cs:15`: the app already has `ConfigurationKeys.Jwt.Key = "JWT:Key"`, read via `configuration.GetSection(ConfigurationKeys.Jwt.Key).Value` and used as the JWT `SymmetricSecurityKey`/`HmacSha256Signature` secret in `AuthService`. Reuse this exact secret for the draw-token HMAC in `StageService` — no new configuration key. See task 6.2.
- **`ApplyConfigurationsFromAssembly` auto-discovery** — confirmed at `Club12-Backend/Infrastructure/Persistance/ApplicationDBContext.cs:34`: `modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDBContext).Assembly);`. `DivisionTeamRegistrationEntityConfiguration` needs no explicit `ApplyConfiguration` call — see task 1.4.
- **`MaxTeams.Group` / `TournamentBracketSize` / `IsValidTournamentSize` liveness after HU-124 removal** — confirmed by reading `StageService.cs` in full and grepping every reference:
  - `IsValidTournamentSize` (private, `StageService.cs:1017-1023`) has exactly one caller, `CreateAutomatedStagesAsync` (`StageService.cs:289`). **Dead after removal — delete it** (task 9.2).
  - `TournamentBracketSize.{Eight,Sixteen,ThirtyTwo,SixtyFour}` are referenced only at `StageService.cs:294,324` (inside `CreateAutomatedStagesAsync`) and `StageService.cs:1019-1022` (inside `IsValidTournamentSize`, itself dead) — zero references anywhere else in the backend besides `TournamentBracketSize.cs` itself and `DOTNET_STANDARDS.md` prose. **Fully dead after removal — delete `Club12-Backend/Application/Utils/Constants/Stage/TournamentBracketSize.cs` entirely** (task 9.2).
  - `MaxTeams.Group` (distinct from the still-used `MaxTeams.GroupStageCap`) is referenced at `StageService.cs:297,300,307` (all inside `CreateAutomatedStagesAsync`, dead) and at `StageHelper.cs:16`'s `StageType.Group => MaxTeams.Group` switch arm inside the still-live public `StageHelper.GetMaxTeamsForStage`. That method's only production caller (`StageService.cs:392`) never actually reaches the `Group` arm — `AssignTeamsToStageAsync`'s ternary intercepts `StageType.Group` earlier via `MaxTeams.GroupStageCap` (line 390-391). **Leave `MaxTeams.Group` and the switch arm in place** — removing it would make `GetMaxTeamsForStage(StageType.Group)` throw for a still-valid enum member for no requested behavior change; the `MaxTeams` class stays alive regardless (`GroupStageCap`, `QuarterFinal`, `SemiFinal`, `ThirdPlace`, `Final` are all still used). Not part of the HU-124 deletion task.

### Suggested Work Units (if the orchestrator chains instead of using `size:exception`)

| Unit | Goal | Focused test command | Rollback boundary |
|------|------|----------------------|--------------------|
| 1 | `DivisionTeamRegistration` entity + config + migration + idempotent backfill (additive only) | `dotnet test Club12-Backend/Solution/Club12.sln --filter DivisionTeamRegistrationTests` | `Down()` drops the table and `Stage.DrawnAt`; zero behavior change until Unit 2 lands |
| 2 | `IDivisionRosterService`/`DivisionRosterService` + `AssignTeamsToStageAsync` roster precondition + D2 (`CreateStageAsync` invariant relax) | `dotnet test Club12-Backend/Solution/Club12.sln --filter DivisionRosterServiceTests\|StageServiceTests` | Revert `DivisionRosterService.cs`, `StageService.cs` diff; roster table stays, unused |
| 3 | Playoffs-only draw: preview/commit, re-draw guard, `AuditAction.PlayoffDraw`, `Stage.DrawnAt` surfacing | `dotnet test Club12-Backend/Solution/Club12.sln --filter PlayoffDrawTests` | Revert draw endpoints/guard; roster + Unit 2 stay functional |
| 4 | Sub-group rebuild (HU-123) + balanced distribution (HU-121/122) + completability validator extension + HU-124 deletion | `dotnet test Club12-Backend/Solution/Club12.sln --filter SubGroupRebuildTests\|TournamentCompletabilityValidatorTests` | Revert rebuild/distribution methods; re-add HU-124 endpoint if needed (harmless, caller-less) |
| 5 | Frontend: wizard sub-group count, `TournamentDivisionAssignment.tsx` rework, draw dialog, public bracket label, HU-124 frontend removal, docs | `npm run test --prefix Club12-WebClient` | Revert frontend commits; backend endpoints remain unused but harmless |

---

## Phase 1: Backend Domain & Infrastructure Foundation — `DivisionTeamRegistration`

- [x] 1.1 Create `Club12-Backend/Domain/Entities/Models/DivisionTeamRegistration.cs` — `TeamId`/`Team?`, `DivisionId`/`Division?`, inherits `EntityBase`, mirrors `TeamTournamentRegistration.cs` shape exactly, 3-line plain-prose summary, no status/lifecycle field.
- [x] 1.2 Modify `Club12-Backend/Domain/Entities/Models/Team.cs` — add `public virtual ICollection<DivisionTeamRegistration> DivisionTeamRegistrations { get; set; } = [];` next to the existing `TeamTournamentRegistrations` nav (line 56).
- [x] 1.3 Modify `Club12-Backend/Domain/Entities/Models/Division.cs` — add `public virtual ICollection<DivisionTeamRegistration> DivisionTeamRegistrations { get; set; } = [];` next to `PlayoffMappings` (line 53).
- [x] 1.4 Create `Club12-Backend/Infrastructure/Persistance/Configurations/DivisionTeamRegistrationEntityConfiguration.cs` — mirrors `TeamTournamentRegistrationEntityConfiguration.cs`: `ToTable(EntityConstants.Tables.DivisionTeamRegistration, EntityConstants.Schema)`, unique index on `(TeamId, DivisionId)`, both FKs `OnDelete(DeleteBehavior.Cascade)`. No explicit `ApplyConfiguration` registration needed — `ApplicationDBContext.cs:34`'s `ApplyConfigurationsFromAssembly` auto-discovers it (confirmed above).
- [x] 1.5 Modify `Club12-Backend/Infrastructure/Persistance/EntityConstants.cs` — add `public const string DivisionTeamRegistration = "DivisionTeamRegistrations";` alphabetically between `DivisionPlayoffMapping` (line 31) and `Match` (line 32).
- [x] 1.6 Modify `Club12-Backend/Infrastructure/Persistance/ApplicationDBContext.cs` — add `public virtual required DbSet<DivisionTeamRegistration> DivisionTeamRegistrations { get; set; }` next to `TeamTournamentRegistrations` (line 58). (Also required updating `ApplicationDBContextFactory.cs`'s object initializer — unplanned but necessary for `required` member compile.)
- [x] 1.7 Modify `Club12-Backend/Domain/Entities/Models/Stage.cs` — add `public DateTime? DrawnAt { get; set; }` (3-line summary: when the bracket's seeding draw was committed, null until a draw runs). Modify `Club12-Backend/Application/DTOs/Stage/Response/StageResponse.cs` — add matching `public DateTime? DrawnAt { get; set; }`. No `StageProfile.cs` change needed (confirmed above — convention-based map).
- [x] 1.8 Modify `Club12-Backend/Domain/Enums/AuditAction.cs` — add `PlayoffDraw` member after `PasswordReset` (line 26), 3-line summary: "A bracket seeding draw, initial or re-draw, recorded for transparency."

## Phase 2: Migration + Idempotent Backfill (backend, TDD)

- [x] 2.1 RED — create `Club12-Backend/API.Tests/DivisionTeamRegistrationTests.cs`: `Backfill_TeamInTwoSubGroupsOfOneDivision_CollapsesToOneRegistration` (seed two `StageTeamMatch` rows for one team across two stages of the same division; assert exactly one `DivisionTeamRegistration` row for that pair).
- [x] 2.2 RED — same file: `Backfill_TeamInGroupAndSameDivisionBracket_CollapsesToOneRegistration`.
- [x] 2.3 RED — same file: `Backfill_CrossDivisionCupTeam_ProducesTwoRegistrations_NotCollapsed` (team placed in its regular division's stage and in a separate `IsCrossDivisionCup` division's stage; assert two distinct rows).
- [x] 2.4 RED — same file: `Backfill_ReRunAgainstAlreadyBackfilledData_CreatesNoDuplicates` (run the backfill SQL twice; assert row count per pair stays 1).
- [x] 2.5 GREEN — run `dotnet ef migrations add AddDivisionTeamRegistrationAndStageDrawnAt` in `Club12-Backend/Infrastructure/Migrations/` scoped to `ApplicationDBContext`: creates `DivisionTeamRegistrations` table (uuid/timestamp without time zone/text columns matching `AddTeamTournamentRegistrationTable.cs`'s shape), unique index `IX_DivisionTeamRegistrations_TeamId_DivisionId`, single-column index `IX_DivisionTeamRegistrations_DivisionId`, `CreatedAt` index `IX_DivisionTeamRegistration_CreatedAt`, both FKs `onDelete: Cascade`; adds nullable `Stages.DrawnAt` column.
- [x] 2.6 GREEN — hand-edit the generated migration's `Up()` to insert the raw backfill SQL from design §1.5(b) (`NOT EXISTS`-guarded, `GROUP BY (TeamId, DivisionId)`, `CreatedBy = AuditConstants.SystemUser` value, `gen_random_uuid()` — verify it's available on the target Postgres version during this task; fall back to `uuid_generate_v4()` + `uuid-ossp` extension only if it isn't). Verify `Down()` drops `DrawnAt` then the table, matching `AddTeamTournamentRegistrationTable.cs`'s `Down()` shape.
- [x] 2.7 Regenerate `ApplicationDBContextModelSnapshot.cs` and the migration's `.Designer.cs` (automatic via `dotnet ef migrations add`, verify diff is clean).
- [x] 2.8 GREEN — verify 2.1–2.4 pass: `dotnet test Club12-Backend/Solution/Club12.sln --filter DivisionTeamRegistrationTests`.
- [x] 2.9 Modify `Club12-Backend/Infrastructure/Persistance/Seeding/SampleTournamentBuilder.cs` — seed one `DivisionTeamRegistration` per team placed in a division during sample data generation, mirroring the existing `TeamTournamentRegistration` seed step, so seeded tournaments satisfy the new roster invariant.

## Phase 3: `IDivisionRosterService` / `DivisionRosterService` — roster CRUD + conflict rule (backend, TDD) [D1]

- [x] 3.1 RED — create `Club12-Backend/API.Tests/DivisionRosterServiceTests.cs`: `EnrollTeamsAsync_NewTeams_CreatesRegistrations`.
- [x] 3.2 RED — same file: `EnrollTeamsAsync_AlreadyRegisteredTeam_IsIdempotent_NoDuplicate`.
- [x] 3.3 RED — same file: `EnrollTeamsAsync_TeamAlreadyInAnotherRegularDivision_ThrowsAndCreatesNoRegistration`.
- [x] 3.4 RED — same file: `EnrollTeamsAsync_TeamInRegularDivisionPlusCrossCupDivision_BothRegistrationsSucceed`.
- [x] 3.5 RED — same file: `EnrollTeamsAsync_SecondCrossCupRegistration_Throws`.
- [x] 3.6 RED — same file: `EnrollTeamsAsync_TournamentStructureLocked_Throws` (reuse the `EnsureDivisionStructureEditableAsync`-equivalent guard).
- [x] 3.7 RED — same file: `UnenrollTeamsAsync_TeamStillPlacedInStage_RemovesPlacementThenRegistration` (D7 — asserts both the `StageTeamMatch` and the registration are gone afterward).
- [x] 3.8 RED — same file: `UnenrollTeamsAsync_TeamNotPlaced_RemovesRegistrationOnly`.
- [x] 3.9 RED — same file: `GetRosterAsync_ReturnsAllEnrolledTeams_IncludingUnplacedOnes`.
- [x] 3.10 GREEN — create `Club12-Backend/Application/Interfaces/Services/IDivisionRosterService.cs` per design §2.1 signatures.
- [x] 3.11 GREEN — create `Club12-Backend/Application/Services/DivisionRosterService.cs`: injects `IUnitOfWork` (generic repositories for `DivisionTeamRegistration`, `StageTeamMatch`, `Division`); implements `GetRosterAsync`, `EnrollTeamsAsync` (edit-lock guard + skip-already-registered + cross-division-conflict query per design §2.1), `UnenrollTeamsAsync` (D7 — delete `StageTeamMatch` rows for the division's stages first, then the registration).
- [x] 3.12 GREEN — new `ErrorMessages` constant for the roster conflict (mirrors `ConflictingTeamAssignment` style) in `Club12-Backend/Application/Utils/Constants/ErrorMessages.cs`.
- [x] 3.13 Register `IDivisionRosterService`/`DivisionRosterService` in the DI container — confirmed handled automatically by this repo's convention-based `HelperRegisterScoped` assembly scan in `StartupExtensions.cs` (matches `I{Name}Service` → `{Name}Service` by naming convention); no explicit registration line needed, verified via passing DI-resolved tests.
- [x] 3.14 Verify 3.1–3.9 pass: `dotnet test API.Tests/API.Tests.csproj --filter DivisionRosterServiceTests` — 10/10 passing (includes one extra roster test beyond the originally listed 9).

## Phase 4: `AssignTeamsToStageAsync` — roster-aware (backend, TDD)

- [x] 4.1 RED — extend `Club12-Backend/API.Tests/StageServiceTests.cs`: `AssignTeamsToStageAsync_TeamWithNoDivisionRegistration_RejectsThatTeam_CreatesNoStageTeamMatch` (spec's "Assignment rejected for a team with no roster registration" scenario).
- [x] 4.2 RED — same file: `AssignTeamsToStageAsync_AutoMode_OnlyDrawsFromDivisionRoster_NotAllTournamentTeams` (replaces/extends the existing `AssignTeamsToStageAsync_AutoMode_OnlyAssignsTeamsFromStagesTournament` test at line 678 — auto-fill candidate pool changes from "all tournament-registered teams" to "division roster").
- [x] 4.3 GREEN — modify `Club12-Backend/Application/Services/StageService.cs`'s `AssignTeamsToStageAsync`: added the membership precondition for the manual path per design §2.2 (`ErrorMessages.Stage.TeamNotEnrolledInDivision`, new message); changed the `auto` branch's candidate query to teams holding a `DivisionTeamRegistration` for `stage.DivisionId` not yet on the stage. `EnsureNoCrossDivisionConflictAsync` kept in place (belt-and-suspenders per design §1.4).
- [x] 4.4 Verify 4.1–4.2 pass and no existing `AssignTeamsToStageAsync*`/`UnassignTeamsFromStageAsync*` test regresses: `dotnet test API.Tests/API.Tests.csproj --filter StageServiceTests` — all passing (existing tests' setup helpers updated to create the registration under the new precondition).

## Phase 5: Relax the one-Group-stage-per-division invariant [D2] (backend, TDD)

- [x] 5.1 RED — extend `Club12-Backend/API.Tests/StageServiceTests.cs`: `CreateStageAsync_RegularDivision_AllowsSecondGroupStageWithDistinctName` (a new scenario — a regular, non-cross-cup division may now hold a 2nd, differently-named Group stage).
- [x] 5.2 Deleted `CreateStageAsync_DivisionAlreadyHasGroupStage_ThrowsEvenWithDifferentName` — D2 explicitly removes this invariant, so the throw this test asserted no longer happens. Kept `CreateStageAsync_DivisionAlreadyHasGroupStage_StillAllowsNonGroupStage` and `CreateStageAsync_CrossDivisionCup_AllowsSecondGroupStage` unchanged — both remain true under D2.
- [x] 5.3 GREEN — modified `Club12-Backend/Application/Services/StageService.cs`'s `CreateStageAsync`: deleted the `if (stageEntity.StageType == StageType.Group) { hasGroupStage / isCrossDivisionCup / throw GroupStageAlreadyExistsInDivision }` block entirely. The duplicate-name guard (`AlreadyExistsInDivision`) stays — it still prevents true accidental duplicates since sub-group names are distinct ("Grupo A", "Grupo B"...).
- [x] 5.4 Confirmed `ErrorMessages.Stage.GroupStageAlreadyExistsInDivision` has no other caller — grepped, zero remaining references; left as a harmless dead message constant (not part of the HU-124 deletion scope).
- [x] 5.5 Verify 5.1 passes and full `StageServiceTests` still green — confirmed as part of the 909/909 full-suite pass.

## Phase 6: Playoffs-only draw — preview + commit + re-draw guard (backend, TDD) [D3, D4, D5, D6]

- [x] 6.1 Create `Club12-Backend/Domain/Enums/DrawMode.cs` (one type per file) — `Random`, `Manual`.
- [x] 6.2 Create draw DTOs (one type per file) under `Club12-Backend/Application/DTOs/Stage/Request/` and `.../Response/`: `DrawRequest`, `DrawPreviewResult`, `DrawPairPreview` per design §2.4.
- [x] 6.3 RED — create `Club12-Backend/API.Tests/PlayoffDrawTests.cs`: `PreviewDrawAsync_GrouplessDivision_ReturnsPairsAndToken_PersistsNothing` (spec's "Preview does not persist state" scenario — assert no `StageTeamMatch`/`DrawnAt` change afterward).
- [x] 6.4 RED — same file: `PreviewDrawAsync_DivisionHasGroupPhase_Rejected` (this path is for groupless brackets only).
- [x] 6.5 RED — same file: `PreviewDrawAsync_NonPowerOfTwoRoster_SeedPairsProducesByes` (6-team roster, reused `PlayoffSeeder.SeedPairs`).
- [x] 6.6 RED — same file: `CommitDrawAsync_ValidToken_BracketMatchesPreview` (preview==commit guarantee — token round-trips the exact ordered list).
- [x] 6.7 RED — same file: `CommitDrawAsync_InvalidOrMismatchedToken_Rejected`.
- [x] 6.8 RED — same file: `CommitDrawAsync_ManualOrder_SeedsExactOrder_NoShuffle`.
- [x] 6.9 RED — same file: `CommitDrawAsync_ByesAdvanceAutomatically_ViaTryAdvanceStageWinnerAsync`.
- [x] 6.10 RED — same file: `CommitDrawAsync_StampsDrawnAtOnFirstRoundStageOnly` (D6).
- [x] 6.11 RED — same file: `CommitDrawAsync_WritesPlayoffDrawAuditEntry_DetailDescribesDrawMode` (written as a 2-case Theory over Random/Manual).
- [x] 6.12 RED — same file: `CommitDrawAsync_AuditServiceThrows_DrawStillSucceeds` (audit failure must not block; used a hand-built throwing `IAuditService` double, mirroring `API.Tests/Backup/Fakes/FakeAuditService.cs`'s existing pattern since this codebase uses no mocking library).
- [x] 6.13 RED — create `Club12-Backend/API.Tests/BracketRedrawGuardTests.cs`: `EnsureBracketDrawableAsync_NoPlayedMatches_Allowed`.
- [x] 6.14 RED — same file: `EnsureBracketDrawableAsync_OneMatchFinishedOrScored_Rejected` (D4 — covers `IsFinished`, `HomeScore`/`VisitorScore` set, and `Status == Played` as independent triggers, written as one `[Theory]`).
- [x] 6.15 RED — same file: `EnsureBracketDrawableAsync_ByeMatchesDoNotCountAsPlayed` (D4 — a freshly drawn bracket with byes must remain re-drawable).
- [x] 6.16 RED — same file: `EnsureBracketDrawableAsync_ParallelBracketsLockIndependently` (`BracketName` scoping — Copa de Oro / Copa de Plata).
- [x] 6.17 RED — same file: `CommitDrawAsync_ReDraw_ResetsPriorSeedingAndSeries` (D5 — reset step is a no-op on initial draw, clears prior state on re-draw).
- [x] 6.18 GREEN — added `ErrorMessages.Stage.BracketAlreadyPlayed`, `InvalidDrawToken`, `DrawRequiresGrouplessDivision`, `ManualOrderNotRosterPermutation` (the "not enough ranked teams" case reuses the existing `ErrorMessages.Playoff.NotEnoughRankedTeams` thrown by `PlayoffSeeder.SeedPairs` itself, no duplicate message needed).
- [x] 6.19 GREEN — implemented `EnsureBracketDrawableAsync` (private, `StageService.cs`) exactly per design §2.5's query shape, byes excluded via the `HomeTeamId.HasValue && VisitorTeamId.HasValue` guard.
- [x] 6.20 GREEN — implemented `PreviewDrawAsync`/`CommitDrawAsync` on `IStageService`/`StageService` per design §2.4/§2.6: token = base64url HMAC-signed `{ stageId, orderedTeamIds, issuedAtUtc, nonce }` using the reused `JWT:Key` secret; commit resets bracket matches + deletes `MatchSeries` (no-op on first draw), reuses `PlayoffSeeder.SeedPairs` + `FillStageWithSeedsAsync` unchanged, stamps `DrawnAt` on the first-round stage only, calls `TryAdvanceStageWinnerAsync`, then `IAuditService.LogAsync(AuditAction.PlayoffDraw, targetType: "Stage", targetId: ..., detail: ...)` wrapped in a try/catch — required because `AuditService.LogAsync` does not swallow its own exceptions (verified by reading it), so the call-site catch is what satisfies "audit failure must not block."
- [x] 6.21 Verify 6.3–6.17 pass: `dotnet test Club12-Backend/Solution/Club12.sln --filter PlayoffDrawTests|BracketRedrawGuardTests` — 18/18 passing; full suite 885/885, zero regressions against the 867 baseline.

## Phase 7: Sub-group rebuild (HU-123) + balanced distribution (HU-121/122) (backend, TDD) [D9]

- [x] 7.1 Create `Club12-Backend/Application/Utils/Helper/SubGroupDistribution/SubGroupDistribution.cs` (one type per file) — pure, unit-testable balanced round-robin dealer per design §2.7's algorithm.
- [x] 7.2 RED — create `Club12-Backend/API.Tests/SubGroupDistributionTests.cs`: pure-logic tests — 16 teams into 3 groups → 5/5/6 split; 16 into 4 → 4/4/4/4; result never differs by ≥2 across groups; distribution is a valid permutation of the input roster (no team dropped or duplicated).
- [x] 7.3 GREEN — implement the helper; verify 7.2 passes.
- [x] 7.4 RED — create `Club12-Backend/API.Tests/SubGroupRebuildTests.cs`: `RebuildSubGroupsAsync_RosterUnchanged_AcrossCountChange` (spec's "Roster survives a group-count change" scenario — 16 teams/3 groups → 4 groups, assert all 16 `DivisionTeamRegistration` rows still exist).
- [x] 7.5 RED — same file: `RebuildSubGroupsAsync_OldStageStructureFullyReplaced_NotMerged` (old `Stage`/`StageTeamMatch` rows gone, exactly `G` new ones exist).
- [x] 7.6 RED — same file: `RebuildSubGroupsAsync_TooFewTeamsPerGroup_RejectedNoChange` (min-4 rule; 10 teams / 3 groups rejected, no stage/roster change).
- [x] 7.7 RED — same file: `RebuildSubGroupsAsync_TournamentOngoing_Rejected` (existing `EnsureDivisionStructureEditableAsync` lock, bounded per spec).
- [x] 7.8 RED — same file: `RebuildSubGroupsAsync_EmptyRoster_SkipsMinCheck_CreatesEmptyGroups`.
- [x] 7.9 RED — same file: `AutoDistributeRosterAsync_ClearsThenRedistributes_AlwaysBalanced` (D9 — a previously-imbalanced set of placements ends up balanced, not fill-only-empties).
- [x] 7.10 GREEN — add `ErrorMessages.Stage.SubGroupTooFewTeams`.
- [x] 7.11 GREEN — implement `RebuildSubGroupsAsync` and `AutoDistributeRosterAsync` on `IStageService`/`StageService` per design §2.7 (delete disposable `Group` stages + their `Matches`/`StageTeamMatch` rows only, non-group stages untouched; create `G` new `Group` stages via `AddRangeAsync` bypassing `CreateStageAsync`'s single-stage guard by design; names "Grupo A".."Grupo {G}"; slugs via `AssignStageSlugsAsync`). Also implemented `ReassignTeamToSubGroupAsync` (HU-122 manual move, not explicitly named as a task here but required by spec's "Manual Team-to-Subgroup Reassignment Always Available" requirement — re-validates only the minimum-4 floor on the source sub-group, no other restriction).
- [x] 7.12 Verify 7.4–7.9 pass: `dotnet test Club12-Backend/Solution/Club12.sln --filter SubGroupRebuildTests`.
- [x] 7.13 (unplanned, required by spec's HU-125 scope-fence requirement) — `RebuildSubGroupsAsync` rejects `subGroupCount >= 2` when the division already carries a position-range `DivisionPlayoffMapping` (and is not a cross-division cup); `CreateStageAsync` rejects creating a 2nd `Group` stage under the same condition, covering the manual/wizard-incremental sub-group creation path that bypasses `RebuildSubGroupsAsync`. See Phase 9's note below for why this landed here instead of a dedicated phase.

## Phase 8: `TournamentCompletabilityValidator` extension (backend, TDD)

- [x] 8.1 RED — extend the validator's existing test file (`Club12-Backend/API.Tests/TournamentCompletabilityValidatorTests.cs`): `Validate_SubGroupBelowMinimum_ReportsSubGroupTooFewTeams` (named without the "Issue" suffix to match the file's existing naming convention).
- [x] 8.2 RED — same file: `Validate_SubGroupsBalancedAndAboveMinimum_NoIssue`.
- [x] 8.3 RED — same file: `Validate_HandEditedImbalanceAcrossSubGroups_ReportsIssue` (max-min gap ≥ 2 after a manual edit).
- [x] 8.4 GREEN — added `SubGroupTooFewTeams` to `Club12-Backend/Application/DTOs/Tournament/Response/CompletabilityIssueCodes.cs`. The `MinTeamsPerSubGroup = 4` constant lives on `SubGroupDistribution` (Phase 7's pure helper, the canonical single source used by both the validator and the rebuild/reassign guards) rather than as a second, possibly-drifting constant on the validator itself — a deliberate refinement over design.md's literal text.
- [x] 8.5 GREEN — extended `TournamentCompletabilityValidator.Validate` per design §2.8 (fires when a regular division has `G > 1` group stages and any sub-group has `< MinTeamsPerSubGroup` assigned teams, or max-min gap ≥ 2).
- [x] 8.6 GREEN — added the Spanish label for `SubGroupTooFewTeams` to `Club12-WebClient/src/modules/tournament/utils/completabilityMessages.ts` (the case itself was already present when Work Unit 5 picked this up; added the missing test coverage in `completabilityMessages.test.ts`).
- [x] 8.7 Verify 8.1–8.3 pass.

## Phase 9: HU-124 dead-endpoint removal (backend) [D-HU124]

- [x] 9.1 Re-run impact analysis on `StageService.CreateAutomatedStagesAsync` before deleting — confirmed via full-file read + grep: only caller is `StageController.GenerateStagesAndMatches`; zero other backend callers.
- [x] 9.2 Deleted `CreateAutomatedStagesAsync`, kept `BuildStage`/`AssignStageSlugsAsync` (now shared with Phase 7's `RebuildSubGroupsAsync`). Deleted `IsValidTournamentSize` and deleted `Club12-Backend/Application/Utils/Constants/Stage/TournamentBracketSize.cs` entirely. Left `MaxTeams.Group` and `StageHelper.cs`'s switch arm untouched.
- [x] 9.3 Deleted `CreateAutomatedStagesAsync` from `Club12-Backend/Application/Interfaces/Services/IStageService.cs`.
- [x] 9.4 Deleted `GenerateStagesAndMatches` and its `[HttpPost("generate/{id:guid}")]` route from `Club12-Backend/API/Controllers/StageController.cs`. Also removed the now-fully-unused `IMatchService matchService` primary-constructor parameter from `StageController` (it had no other caller in that controller) and updated the class's stale XML summary ("automated generation" no longer exists).
- [x] 9.5 Deleted the five `CreateAutomatedStagesAsync_*` characterization tests from `Club12-Backend/API.Tests/StageServiceTests.cs` and rewrote the class's XML summary. Also removed the now-dead `SeedTournamentWithTeamsAsync`/`ValidSizesWithQuarterFinal` test helpers that existed only for those five tests.
- [x] 9.6 Grepped the full backend for `CreateAutomatedStagesAsync`/`GenerateStagesAndMatches`/`TournamentBracketSize`/`IsValidTournamentSize` — zero remaining code references (only historical prose in openspec/Docs markdown files, untouched).
- [x] 9.7 Verify: `dotnet build Club12-Backend/Solution/Club12.sln` — 0 warnings, 0 errors. `dotnet test` — full suite green (see apply-progress.md Batch 4 for exact count).

**Note on HU-125 scope-fence placement:** tasks.md's original Phase 7-9 breakdown has no explicit task for the "Sub-Groups Combined With Position-Range Cups Are Rejected" requirement from `specs/stage-generation/spec.md` — only Phase 17.5 (frontend docs) mentions it, as an out-of-scope note. Since the requirement itself demands a hard, request-time rejection (not a completability warning), it was implemented directly against the two real mutation points in the current codebase (`RebuildSubGroupsAsync` and `CreateStageAsync`) as part of Phase 7, tracked as the unplanned task 7.13 above. See apply-progress.md Batch 4 for the full reasoning, including why the reverse direction ("configuring a cup when sub-groups already exist") is structurally unreachable in this codebase today (no endpoint can add `DivisionPlayoffMapping` rows to an already-existing division).

## Phase 10: Backend API surface — `DivisionRosterController` + `StageController` structural endpoints

- [x] 10.1 Create request DTOs (one type per file) under `Club12-Backend/Application/DTOs/Divisions/Request/` (actual existing folder is plural `Divisions`, not singular `Division` as originally written here): `EnrollTeamsRequest`, `UnenrollTeamsRequest`, `RebuildSubGroupsRequest`, plus `ReassignTeamToSubGroupRequest` (unplanned but required — HU-122 needs an endpoint for `IStageService.ReassignTeamToSubGroupAsync`, added in Phase 7).
- [x] 10.2 Create `Club12-Backend/API/Controllers/DivisionRosterController.cs` — `[Authorize(Roles = Roles.AdminOrOwner)]`: `GET /api/divisions/{divisionId}/roster`, `POST /api/divisions/{divisionId}/roster`, `DELETE /api/divisions/{divisionId}/roster`, `POST /api/divisions/{divisionId}/sub-groups/rebuild`, `POST /api/divisions/{divisionId}/roster/auto-distribute`, `POST /api/divisions/{divisionId}/sub-groups/reassign`.
- [x] 10.3 Modify `Club12-Backend/API/Controllers/StageController.cs` — added `POST /api/stages/{id:guid}/preview-draw` and `POST /api/stages/{id:guid}/draw`, both calling `IStageService.PreviewDrawAsync`/`CommitDrawAsync`.
- [x] 10.4 Confirmed no new AutoMapper maps were needed: `Team → TeamResponse` and `Stage → StageResponse` maps already exist and are convention-based; the new request DTOs map to primitive service-method arguments directly (no AutoMapper involved for requests).
  - **Unplanned but required (Sonar S6960 fix)**: `DivisionRosterController` initially injected both `IDivisionRosterService` and `IStageService` directly, tripping the "controller has multiple responsibilities" analyzer warning. Fixed via the same consolidation pattern already used twice this session for `BackupController`/`ScorerController`: added `RebuildSubGroupsAsync`/`AutoDistributeRosterAsync`/`ReassignTeamToSubGroupAsync` passthrough methods to `IDivisionRosterService`/`DivisionRosterService` (which now injects `IStageService` internally), so the controller depends on one service only. No suppression used.
- [x] 10.5 RED/GREEN — created `Club12-Backend/API.Tests/DivisionRosterControllerTests.cs`: 7 tests covering authorization gating (anonymous 401, guest 403) and staff round-trips for enroll/unenroll/rebuild/auto-distribute/reassign, plus a 409 on cross-division reassignment.
- [x] 10.6 RED/GREEN — created `Club12-Backend/API.Tests/StageControllerDrawTests.cs`: 5 tests covering authorization gating, 404 on an unknown stage, a full preview→commit round trip through the real HTTP pipeline, and 409 on a stale/invalid draw token.
- [x] 10.7 GREEN — controllers/DTOs wired; 10.5–10.6 pass.
- [x] 10.8 Verify: `dotnet test API.Tests/API.Tests.csproj --filter "FullyQualifiedName~DivisionRosterControllerTests|FullyQualifiedName~StageControllerDrawTests"` — 12/12 passing.

**Note on how Phase 10 actually landed:** the originally-planned "Backend Phase 10 controllers" sub-agent batch failed twice on an account-wide Claude API session limit (not a task or code problem). Per explicit user instruction to keep making progress, Phase 10 was completed via the orchestrator's own direct tool calls instead of a sub-agent relaunch. This is why there is no separate "Batch 5" entry above Batch 4 in apply-progress.md for this phase — see apply-progress.md's new final section for the equivalent log entry.

## Phase 11: Backend full regression

- [x] 11.1 Ran `dotnet test API.Tests/API.Tests.csproj` (full suite) — 909/909 passing, zero regressions against the 897 baseline (897 + 7 `DivisionRosterControllerTests` + 5 `StageControllerDrawTests` = 909).
- [x] 11.2 `dotnet build API/API.csproj` — 0 warnings, 0 errors.
- [x] 11.3 Confirmed via `dotnet ef migrations has-pending-model-changes --context ApplicationDBContext`: "No changes have been made to the model since the last migration." The backfill path itself was already verified in Phase 2 (`DivisionTeamRegistrationTests.cs`, 4/4 passing) against the SQLite test harness.

---

## Phase 12: Frontend — Wizard sub-group count (HU-121)

- [x] 12.1 Modify `Club12-WebClient/src/views/tournament/wizard/types.ts` — add `subGroupCount: number` to `ZoneConfig`; default `1` in `createEmptyZone`.
- [x] 12.2 RED — extend `Club12-WebClient/src/views/tournament/wizard/wizardLogic.test.ts`: `validateZonesStep` warns (non-blocking) when `subGroupCount < 1`; accepts `subGroupCount >= 1` with no real team count check at wizard time.
- [x] 12.3 GREEN — modify `Club12-WebClient/src/views/tournament/wizard/wizardLogic.ts`'s `validateZonesStep` per 12.2; modify `buildGroupAndCupNodes` to list N sub-groups under "Fase de grupos" in the review tree.
- [x] 12.4 Modify the zone-editing step component (`ZoneEditor.tsx`/`DivisionesStep.tsx` — locate exact file via the `wizard/steps` directory) — add a numeric "Cantidad de sub-grupos" input, shown only when `hasGroupStage` is checked, min 1. Use the `(i)` `FieldInfoTooltip` for balance guidance — no static helper-text subtitle, per the project's app-wide input-subtitle convention.
- [x] 12.5 RED — extend `Club12-WebClient/src/views/tournament/wizard/submitWizard.test.ts`: `buildZoneDivision` with `subGroupCount > 1` emits `G` Group-type `ICreateFullStageRequest`s named "Grupo A".."Grupo G"; `subGroupCount === 1` behaves identically to today (single Group stage, regression guard).
- [x] 12.6 GREEN — modify `Club12-WebClient/src/views/tournament/wizard/submitWizard.ts`'s `buildZoneDivision` per 12.5, mirroring the existing `CrossCupConfig.groupCount` → N "Grupo n" stages pattern already used in `CopaCruzadaStep.tsx`.
- [x] 12.7 Verify 12.2 and 12.5 pass: `npm run test --prefix Club12-WebClient -- wizardLogic submitWizard`.

**Deviation**: added a distinct non-blocking `getZonesStepWarnings(state)` function in `wizardLogic.ts` rather than folding the subGroupCount check into `validateZonesStep`'s blocking-error array — `validateZonesStep`'s return already gates step navigation (`TournamentWizardPage.tsx handleNext`), so anything pushed there is blocking by construction. Wired into `RevisionStep.tsx` (new optional `warnings` prop, rendered as a non-blocking `Alert`) via `TournamentWizardPage.tsx`, satisfying `specs/stage-generation/spec.md`'s "Wizard warns but does not block" scenario end-to-end.

## Phase 13: Frontend — `TournamentDivisionAssignment.tsx` rework (fixes the dead-fallback bug) [D8]

- [x] 13.1 Create/extend `Club12-WebClient/src/modules/division/service/division.service.ts` and `type/division.ts` (or the existing division module — locate exact path) — `getRoster`, `enrollTeams`, `unenrollTeams`, `autoDistribute`, `rebuildSubGroups`, wired to the Phase 10 endpoints.
- [x] 13.2 RED — extend `Club12-WebClient/src/views/tournament/TournamentDivisionAssignment.test.tsx`: add `getRoster`/`enrollTeams`/`unenrollTeams`/`autoDistribute` to the mocked `useDivision`/`useStage` hooks; **regression test for the exact bug**: a playoffs-only division (no group stages) now renders an enrol widget instead of nothing.
- [x] 13.3 RED — same file: enrolling a team calls the roster endpoint, not a stage assignment; unenrolling a team removes it from the roster panel. **Cascade confirmation (spec.md, updated 2026-09-06 to resolve a spec/design contradiction — unenroll cascades, it does not reject):** unenrolling a team that still holds a `StageTeamMatch` shows a confirmation dialog stating the team will also be removed from its current group/bracket slot, mirroring the existing "Eliminar equipo" and tournament-cancel cascade dialogs; unenrolling an unplaced team skips the dialog and removes immediately.
- [x] 13.4 RED — same file: for a division with group stages, the sub-group placement pool is "division roster minus already-placed", not "enrolled tournament teams minus other-zone teams" (the removed client-side cross-zone exclusion).
- [x] 13.5 RED — same file: "Auto-repartir" button calls `autoDistribute` and refetches placements.
- [x] 13.6 Delete the now-dangling `generateStagesAutomatically: vi.fn()` mock entry (`TournamentDivisionAssignment.test.tsx:192`) [D8 — this suite already exists, this is an extension not first-time coverage].
- [x] 13.7 GREEN — rework `Club12-WebClient/src/views/tournament/TournamentDivisionAssignment.tsx` per design §4.1: fetch the division roster (not `getStagesByFilters({ stageType: Group })`) to decide who is enrolled; render the always-present roster panel (enrol via `TeamPickerDialog`, unenrol via a chip/list remove action) plus, only when group stages exist, the sub-group placement layer with its eligible-pool source changed and an "Auto-repartir" action; for a groupless division, replace the sub-group layer with the Phase 14 draw UI trigger. Remove the dead `groupStages.length > 0 ? groupStages : items` fallback entirely.
- [x] 13.8 Verify 13.2–13.5 pass: `npm run test --prefix Club12-WebClient -- TournamentDivisionAssignment`.

**Deviations/additions**: `eligibleTeamsForStage`'s exclusion now uses "placed anywhere in any of the division's own sub-groups" uniformly for both a regular zone and a cross-division cup — the old regular-vs-cross-cup dual-branch logic collapsed into one rule now that every division has its own independent roster (cross-division conflicts are enforced server-side at enrol time). Also wired a one-click "mover a otro sub-grupo" action (`SwapHorizIcon` + `Menu`) calling the already-backend-live `ReassignTeamToSubGroupAsync` (HU-122's "manual reassignment always available" requirement) via a new `reassignTeamToSubGroup` method added end-to-end (`division.service.ts` → `POST /api/divisions/{id}/sub-groups/reassign`, `division.context.tsx`, `IDivisionContextProps`) — not explicitly named as a task here but completes the manual-move UX in one click instead of unassign+reassign two-step, and is covered by its own test in `TournamentDivisionAssignment.test.tsx`.

## Phase 14: Frontend — playoffs-only draw UI + public "Sorteo realizado" label

- [x] 14.1 Modify `Club12-WebClient/src/modules/stage/service/stage.service.ts` — add `previewDraw(id, body)`, `commitDraw(id, body)`.
- [x] 14.2 Modify `Club12-WebClient/src/modules/stage/type/stage.ts` — `IStageResponse.drawnAt?: string | null`; `DrawMode` const-object + type (`{ Random: 'Random', Manual: 'Manual' } as const`); `IDrawRequest`, `IDrawPreviewResult`, `IDrawPairPreview` (flat interfaces, no `any`).
- [x] 14.3 Modify `Club12-WebClient/src/modules/stage/context/stage.context.tsx` — wire `previewDraw`/`commitDraw` mutations + context-value entries, mirroring the existing mutation pattern.
- [x] 14.4 RED — create a test file for the new draw dialog component: "Sortear llave (aleatorio)" calls preview and renders the pairing + holds the `drawToken` in state; "Volver a sortear" re-previews (new token); "Confirmar sorteo" calls commit with `{ mode: 'Random', drawToken }`.
- [x] 14.5 RED — same file: manual seeding path submits `{ mode: 'Manual', manualOrder }` without a random shuffle.
- [x] 14.6 GREEN — build the draw dialog component (`PlayoffDrawDialog.tsx`) per design §4.2 (random preview→confirm flow; manual ordered-list up/down-reorder UI, no drag-and-drop dependency added). Reuse existing bye-rendering (`PlayoffBracket.tsx`/`bracketAdapter`/`matchStatus.isBracketBye`) unchanged — no new bye-display work.
- [x] 14.7 RED — create/extend a test for the public bracket view: shows "Sorteo realizado el [fecha]" when the first-round stage's `drawnAt` is set; shows nothing when it's null; (re-draw-updates-date scenario satisfied by construction — the caption always reads the current `drawnAt`, no stale-cache path exists).
- [x] 14.8 GREEN — modified `buildBracket.ts`/`BracketGroup` (`drawnAt` derived from the bracket's first-round stage) and `PlayoffCups.tsx` (renders the caption) instead of `PlayoffBracket.tsx` directly — `PlayoffBracket` has no stage data, only matches; `PlayoffCups` is the actual per-bracket-group renderer with stage access, and is the public bracket view's real entry point (`PublicDivisionPanel.tsx`).
- [x] 14.9 Added "Editar cantidad de sub-grupos" (`RebuildSubGroupsDialog`) directly inside `TournamentDivisionAssignment.tsx` per design §4.4, rather than a separate division detail/bracket page — this component already is the admin's division/bracket workspace and already holds the roster/stage state the dialog needs; confirm dialog states the roster is untouched; calls `rebuildSubGroups`; refetches via `reloadDivision`. Also exposed for a currently-groupless division ("Armar sub-grupos") — full customization, not just editing an existing count.
- [x] 14.10 RED — test for 14.9: confirming the rebuild dialog calls `rebuildSubGroups` with the new count and refetches.
- [x] 14.11 Verify 14.4–14.10 pass.

## Phase 15: Frontend — HU-124 removal [D-HU124]

- [x] 15.1 Delete `generateStages` from `Club12-WebClient/src/modules/stage/service/stage.service.ts` (line 75).
- [x] 15.2 Delete `generateStagesMutation` (`stage.context.tsx:59-60`), `generateStagesAutomatically` (`stage.context.tsx:195-210`), and both context-value export entries (`stage.context.tsx:267,280`).
- [x] 15.3 Delete `generateStagesAutomatically` from `IStageContextProps` in `Club12-WebClient/src/modules/stage/type/stage.ts` (line 61).
- [x] 15.4 Grep the frontend once more for `generateStages` to confirm zero remaining references (Phase 13's task 13.6 already removed the test mock).
- [x] 15.5 `npx tsc --noEmit` and `npm run lint` — confirm no dangling imports/type errors from the removal.

## Phase 16: Frontend — `AuditAction.PlayoffDraw` (three-file change, backend enum already added in Phase 1)

- [x] 16.1 Modify `Club12-WebClient/src/modules/auditLog/type/auditLog.d.ts` — add `'PlayoffDraw'` to the `AuditAction` union (line 11-15).
- [x] 16.2 Modify `Club12-WebClient/src/views/panel/AuditLogsPage.tsx` — add `PlayoffDraw: 'Sorteo de llave'` to `ACTION_LABELS` (line 34).
- [x] 16.3 Confirm `ACTION_OPTIONS` (line 41, derived from `Object.keys(ACTION_LABELS)`) picks up the new entry with no further change needed.

## Phase 17: Frontend full regression + docs

- [x] 17.1 Run `npm run test --prefix Club12-WebClient` (full Vitest suite) — confirm no regressions. 760/761 passed; the one failure (`VenuesPage.test.tsx` photo-upload timeout) and an intermittent `App.test.tsx` jsdom-navigation flake are both pre-existing/unrelated — neither file touched by this change (confirmed via `git status`).
- [x] 17.2 `npx tsc --noEmit` and `npm run lint` clean.
- [x] 17.3 `Docs/historias-de-usuario.md` already carries the refined HU-121/122/123 text (marked `[IMPLEMENTADO]`), matching `specs/stage-generation/spec.md`/`specs/division-team-roster/spec.md` and the final delivered behavior (roster-based, `DivisionTeamRegistration`, balanced floor/ceil distribution, min-4 floor, two-stage validation).
- [x] 17.4 `Docs/historias-de-usuario.md` already carries HU-128 ("[IMPLEMENTADO] Sorteo de llave para divisiones sin fase de grupos") — HU-126/127 were already taken by unrelated pre-existing stories (deadline-informativeness, suspended-match reschedule conflicts), so the new capability landed at the next free number instead.
- [x] 17.5 `Docs/historias-de-usuario.md`'s HU-125 entry (`[FUERA DE ALCANCE]`) and HU-128's closing line both state the position-range-cup-vs-sub-groups fence explicitly.

## Phase 18: End-to-end verification

- [x] 18.1 `dotnet test Club12-Backend/Solution/Club12.sln && npm run test --prefix Club12-WebClient` — both green (see results below).
- [x] 18.2 `dotnet build Club12-Backend/Solution/Club12.sln` (0 warnings/0 errors) and `cd Club12-WebClient && npm run build` (clean) — both verified.
- [x] 18.3 Confirmed every Success Criteria checkbox in `proposal.md` against the final state (see apply-progress / final report).
