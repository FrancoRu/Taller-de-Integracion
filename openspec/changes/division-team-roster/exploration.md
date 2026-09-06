# Exploration: division-team-roster

Status: exploration complete, no design decisions made. Scope per the background
brief: (1) `DivisionTeamRegistration` entity + migration, (2) HU-121/122/123
sub-group refinement, (3) playoffs-only division seeding (random draw / manual,
preview, audit, re-draw lock), (4) keep the data model open to split-league,
consolation bracket, repechaje, and (tentatively) Swiss — none of those four are
being built now.

## 1. The bug, confirmed mechanically

`Club12-WebClient/src/views/tournament/TournamentDivisionAssignment.tsx:289-299`:

```tsx
const stagesResult = await getStagesByFilters({
  divisionId: division.id,
  stageType: StageType.Group,
  pageSize: FILTER_OPTIONS_PAGE_SIZE,
});

const items = stagesResult?.items ?? [];
const groupStages = items
  .filter(stage => stage.stageType === StageType.Group)
  .sort((a, b) => a.order - b.order);
const resolvedStages = groupStages.length > 0 ? groupStages : items;
```

`getStagesByFilters` sends `stageType: Group` to the server, so `items` is
**already** filtered server-side — `items` can never contain anything but Group
stages. When a division has no Group stage (wizard's `hasGroupStage` unchecked),
`items` is `[]`, `groupStages` is `[]`, and `resolvedStages` is `[]` — the
`groupStages.length > 0 ? groupStages : items` fallback is dead code, it can
never reach a non-empty branch. The division renders with zero assignable
groups, so a playoffs-only division has literally no widget to add a team to.

This is a UI-side symptom of a deeper structural gap: confirmed below.

## 2. Backend entity relationships (current state)

```
Team ──< StageTeamMatch >── Stage ── Division ── Tournament
Team ──< TeamTournamentRegistration >── Tournament   (roster: team ↔ tournament)
Team ──< PlayerTeamRegistration >── Player            (roster: player ↔ team, per tournament)
```

`Domain/Entities/Models/Team.cs` has **no `DivisionId` and no `Division`
navigation property at all.** The only path from a Team to a Division is
`Team → StageTeamMatch → Stage → Division`. There is no division-level roster
independent of stage attachment — this is the confirmed root cause named in
the background brief, verified by reading the entity directly.

`StageTeamMatch` (`Domain/Entities/Models/StageTeamMatch.cs`, config in
`Infrastructure/Persistance/Configurations/StageTeamMatchEntityConfiguration.cs`)
is a bare join row: `StageId` (FK, cascade), `TeamId` (FK, cascade), no unique
constraint beyond the two FKs, extends `EntityBase`. It is created/removed
exclusively through `StageService`:

- `AssignTeamsToStageAsync(Stage, List<Guid>? teamIds, bool auto)` —
  `Application/Services/StageService.cs:383` — manual (explicit team ids) or
  automatic (fills open slots from tournament-registered teams not already on
  this stage) team-to-stage assignment. Guards: `EnsureDivisionStructureEditableAsync`
  (blocks once `Ongoing`/`Finished`/`Canceled`), a slot-capacity check
  (`MaxTeams.GroupStageCap` = 32 for Group stages, `StageHelper.GetMaxTeamsForStage`
  otherwise), and `EnsureNoCrossDivisionConflictAsync` — a team already on a
  stage in a **different, non-cross-cup division** of the same tournament is
  rejected, but a team may simultaneously sit in its home zone AND a
  cross-division cup (`Division.IsCrossDivisionCup`). This "one team, one
  regular zone, plus optionally one cross-cup" invariant is currently enforced
  entirely by querying `StageTeamMatch` joined to `Stage.Division` — **any
  replacement roster model must preserve this rule or explicitly change it.**
- `UnassignTeamsFromStageAsync(Stage, List<Guid>)` — same edit-lock guard,
  straight delete of matching `StageTeamMatch` rows.

Both are called from `StageController` and, on the frontend, from
`TournamentDivisionAssignment.tsx`'s `handleAdd`/`handleRemove` via
`useStage()`'s `assignTeamsToStage`/`unassignTeamsFromStage`
(`Club12-WebClient/src/modules/stage/service/stage.service.ts:85-103`, routes
`POST /api/stages/{id}/assign-team`, `DELETE /api/stages/{id}/unassign-team`).

### The registration pattern to mirror

`TeamTournamentRegistration` (`Domain/Entities/Models/TeamTournamentRegistration.cs`)
is the simplest, most directly analogous precedent:

```csharp
public class TeamTournamentRegistration : EntityBase
{
    public required Guid TeamId { get; set; }
    public Team? Team { get; set; }
    public required Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
}
```

Configuration (`TeamTournamentRegistrationEntityConfiguration.cs`): table name
`TeamTournamentRegistrations` (constant in `EntityConstants.Tables`), unique
index on `(TeamId, TournamentId)`, both FKs `OnDelete(DeleteBehavior.Cascade)`.
`BaseEntityConfiguration<TEntity>` (shared base) wires `Id` (key,
`ValueGeneratedOnAdd`), `DateCreated` (required) + `IX_{TypeName}_CreatedAt`
index, `DateUpdated` (nullable) — `CreatedBy`/`UpdatedBy` come from
`EntityBase` itself and are configured implicitly. `PlayerTeamRegistration`
is the richer sibling (adds a status enum stored `HasConversion<string>()`,
a second unique index for jersey numbers) — useful if `DivisionTeamRegistration`
ever needs status beyond "registered" (see open questions).

Migration precedent: `Infrastructure/Migrations/20260827170738_AddTeamTournamentRegistrationTable.cs`
— `CreateTable` with the same five audit columns
(`Id`, `DateCreated`, `DateUpdated`, `CreatedBy`, `UpdatedBy`), two FKs with
`onDelete: Cascade`, three indexes (`CreatedAt`, the unique compound pair, and
a single-column index on the "many" side FK). Migration file naming is
`yyyyMMddHHmmss_DescriptiveName.cs` + paired `.Designer.cs`, plus a diffed
`ApplicationDBContextModelSnapshot.cs`. Most recent migration as of this
exploration: `20260902173728_AddTeamShirtTertiaryColor`.

### Structural edit lock (relevant to HU-123 and re-draw)

`EnsureDivisionStructureEditableAsync` (private,
`Application/Services/StageService.cs:160`) is the single existing lock point:

```csharp
private async Task EnsureDivisionStructureEditableAsync(Guid divisionId)
{
    Division? division = await _divisionRepository.GetByIdAsync(
        divisionId, includes: [division => division.Tournament]);
    if (division?.Tournament is null) return;
    bool structureLocked = division.Tournament.Status
        is TournamentStatus.Ongoing or TournamentStatus.Finished or TournamentStatus.Canceled;
    if (structureLocked)
        throw new InvalidOperationException(ErrorMessages.Stage.StructureLockedTournamentStarted);
}
```

Called from `CreateStageAsync`, `UpdateStageAsync`, `DeleteStageAsync`,
`AssignTeamsToStageAsync`, `UnassignTeamsFromStageAsync`. It locks at the
**tournament** level (any of the three terminal/active statuses), not at
"has this specific bracket's first match been played" granularity. The
background brief's requirement for playoff re-draw ("allowed only before the
first match of the bracket has been played") is **stricter and different**
from this existing lock — it needs a new, bracket-scoped check (e.g. "no
match in this Stage/BracketName has `IsFinished` or has a recorded score/date
started"), not a reuse of `EnsureDivisionStructureEditableAsync` as-is. HU-123
("change group count before tournament starts") maps cleanly onto the
existing tournament-level lock, though.

## 3. HU-121/122/123 (`Docs/historias-de-usuario.md:738-812`)

- **HU-121**: organizer picks a group *count* (not a fixed group size); teams
  split as evenly as possible (`floor(T/G)`/`ceil(T/G)`, never a gap ≥ 2).
  Minimum 4 teams per sub-group (reject smaller). Validated twice: non-blocking
  warning in the wizard (before real enrollment numbers exist), blocking at
  completability-guard time (`TournamentCompletabilityValidator`, HU-109's
  extension point). **Owner clarification dated 2026-09-05**: the group count
  chosen while building structure is a *starting point*, not final — HU-121/122/123
  are one combined scope, not HU-121 alone with the other two as a nice-to-have.
- **HU-122**: auto-assign by default (random-balanced — no reliable
  cross-season strength ranking for an amateur league, flagged as a product
  decision not a technical one), manual reassignment always available.
  Extends the *existing* `TournamentDivisionAssignment.tsx` +
  `TeamPickerDialog` flow — today's gap is one stage/group per division to pick
  from; with sub-groups, the same picker UI works, it just needs an
  "auto-distribute" one-click action layered on top of manual per-group
  adjustment.
- **HU-123**: change sub-group count after structure exists but before the
  tournament starts. **No such action exists today** — the only current lever
  is `StageService.UpdateStageAsync` (edits one stage's fields) or manually
  deleting/creating stages, neither of which rebalances or reassigns teams.
  Must respect the same tournament-status lock as `EnsureDivisionStructureEditableAsync`.
  Changing the count with teams already placed must re-trigger the HU-121/122
  balanced distribution, never leave teams orphaned.

**Why `DivisionTeamRegistration` makes HU-123 materially cleaner**: today,
"a team is in this division" and "a team is in this specific sub-group Stage"
are the *same fact* (one `StageTeamMatch` row). Changing group count means
destroying and rebuilding `Stage` rows, which — with the current model —
necessarily also destroys the only record that a team belonged to the division
at all, forcing a rebuild of division membership *and* re-distribution into
new stages as one entangled operation. With a `DivisionTeamRegistration`
roster as the durable fact ("team X is enrolled in division Y"), a group-count
change becomes: keep the roster untouched, delete/rebuild only the `Stage` +
`StageTeamMatch` layer, then re-run the balanced-distribution algorithm over
the *unchanged* roster. This is the concrete mechanism behind the brief's "the
roster should survive a structure change; only the group-stage attachment gets
rebuilt."

HU-124 (documented tech debt, same file, lines 791-811) is directly relevant
risk: `StageService.CreateAutomatedStagesAsync` /
`POST /api/stages/generate/{id}` is a second, contradictory, currently-orphaned
(no UI caller) mechanism for "build group stages" that hardcodes
`MaxTeams.Group = 4`-team groups and requires exactly 8/16/32/64 registered
teams — incompatible with HU-121's organizer-chosen group count. The propose
phase should explicitly decide whether this dead endpoint gets deleted or
retrofitted, since leaving it alive under the same conceptual name
("generate group stages") is a foot-gun for whoever implements HU-121 next to
wire the wrong one by accident.

## 4. Playoffs-only division seeding — deeper problem than the UI bug

`SeedKnockoutStageAsync` (`Application/Services/StageService.cs:527-586`) is
the **only** existing seeding path for a first-round elimination stage, and it
structurally cannot serve a playoffs-only division, independent of the UI bug:

```csharp
List<Guid> assignedTeamIds = [.. stage.StageTeamMatches.Select(stm => stm.TeamId)];
...
List<Match> groupMatches = [.. await _matchRepository.FindAsync(m =>
    m.Stage.DivisionId == stage.DivisionId && m.Stage.StageType == StageType.Group, ...)];
List<Position> standings = PositionCalculator.CalculatePositions(
    groupMatches, stage.Division.PointsForWin, stage.Division.PointsForLoss);
List<Guid> orderedTeamIds = [.. standings
    .Where(position => assignedTeamIds.Contains(position.TeamId))
    .Select(position => position.TeamId)];
if (orderedTeamIds.Count != assignedTeamIds.Count)
    throw new InvalidOperationException(ErrorMessages.Stage.SeedMissingStandings);
```

For a division with **no Group stage**, `groupMatches` is always empty, so
`standings` is always empty, so `orderedTeamIds` is always empty, so unless
`assignedTeamIds` is *also* empty this always throws `SeedMissingStandings`.
**Even if a team could be assigned directly to the knockout `Stage` today
(bypassing the wizard bug via, say, a Swagger call to
`AssignTeamsToStageAsync`), seeding that bracket would still be impossible**
with the current method — there is no strength signal to seed from because
there was never a group phase. This confirms scope item 3 needs a genuinely
new seeding path, not a parameter tweak to the existing one.

`PlayoffSeeder.SeedPairs` (`Application/Utils/Helper/Playoff/PlayoffSeeder.cs`)
is, however, directly reusable — it is a pure function over any
best-seed-first `IReadOnlyList<Guid>`, with no knowledge of where the order
came from:

```csharp
public static List<(Guid HomeTeamId, Guid? VisitorTeamId)> SeedPairs(IReadOnlyList<Guid> orderedTeamIds)
```

It pads to the next power of two with `null`s (`NextPowerOfTwo`), builds the
classic recursive bracket seed order (`BuildSeedOrder`, 1-vs-N pairing so top
seeds only meet in the final), and a `null` second slot means a bye — the home
team walks over. This is exactly the "best-seed-gets-a-bye" behavior the brief
references and it needs zero changes to serve a random-draw or manual-seed
path: the new mechanism only has to *produce* an `orderedTeamIds` list (a
`Random`-shuffled order for "sorteo aleatorio", or an admin-specified order for
manual seeding) and hand it to the same `SeedPairs`/`FillStageWithSeedsAsync`
machinery already used for group-standings seeding.

Also reusable/mirror-worthy: `FillStageWithSeedsAsync` (private, populates a
stage's pre-generated empty `Match` rows from `SeedPairs`' output, then
`TryAdvanceStageWinnerAsync` immediately pushes a bye's implicit winner into
the next round). `CrossCupGroupSeeder.ResolveSeedOrder` is a second precedent
for "produce an ordered team list from something other than direct division
standings" (pools top-N per group across a multi-group cross-cup) — same shape
of problem as "produce an order from a manual/random draw," worth reviewing
for API-shape consistency (both take/return `List<Guid>` orderings).

### Draw preview, re-draw lock, and audit — none of this exists yet

- **Preview before commit**: no existing "preview a mutation before persisting
  it" pattern was found in `StageService`. HU-121 already establishes a
  *different* preview pattern (show the balanced-group split before confirming,
  entirely client-side/derived) — the propose phase should decide whether a
  draw preview is computed server-side (a stateless `POST .../preview-draw`
  that returns the pairing without writing anything) or client-side (the
  frontend runs the same random-shuffle algorithm locally, shows it, then
  submits the same order back to commit) — this affects whether "the shown
  preview is guaranteed to match what gets committed" (server-side) or not
  (client-side, unless the exact same seed/order round-trips).
- **Re-draw lock scoped to "first match played," not tournament status**:
  confirmed above — `EnsureDivisionStructureEditableAsync` is the closest
  existing analog in *shape* (a guard method thrown as `InvalidOperationException`
  → mapped to 409) but wrong in *scope* (whole-tournament status, not
  per-bracket match state). A new guard is needed, checking whether any
  `Match` in the target stage/bracket has `IsFinished` or a non-null
  score/date-played.
- **Audit logging**: `IAuditService.LogAsync(AuditAction, targetType?, targetId?, targetName?, detail?, ct)`
  (`Application/Interfaces/Services/IAuditService.cs`) is the trail to extend.
  `AuditAction` (`Domain/Enums/AuditAction.cs`) currently has exactly four
  values: `DataWipe`, `BackupRestore`, `TournamentStatusChange`, `PasswordReset`
  — a bracket draw needs a **new enum member** (e.g. `PlayoffDraw` or
  `BracketSeeding`), which is a three-file change: the backend enum, the
  frontend `AuditAction` type alias
  (`Club12-WebClient/src/modules/auditLog/type/auditLog.d.ts:11-15`, a plain
  string union, not generated from the backend), and `ACTION_LABELS` in
  `AuditLogsPage.tsx:34-39` (Spanish label map). `AuditLog` already carries
  `TargetType`/`TargetId`/`TargetName`/`Detail` — a draw entry would set
  `TargetType = "Division"` or `"Stage"`, and `Detail` could hold something
  like "Sorteo aleatorio — 8 equipos" or the manual order description.
  `AuditService.LogAsync` never throws at the call site (fire-and-forget
  pattern already established for `TryAutoSeedPlayoffPhaseAsync`'s style of
  resilience) — worth mirroring so a logging failure never blocks the actual
  draw.
- **"Sorteo realizado el [fecha]" on the public bracket view**: no existing
  field carries this. Two options worth flagging for propose: (a) a new
  nullable `Stage.DrawnAt` (or similar) column set at draw time, surfaced
  through `IStageResponse`, or (b) derive it by querying the audit trail for
  the latest `PlayoffDraw`/`BracketSeeding` entry targeting that
  stage/division — the audit trail is currently Admin/Owner-only
  (`[Authorize(Roles = Roles.AdminOrOwner)]` on `AuditLogController`), so a
  **public**-facing "sorteo realizado el…" label cannot read from
  `GET /api/audit-logs` as-is; either a public read needs to be carved out or
  (a) is the simpler, more consistent choice with how the rest of the public
  bracket view already gets its data (`IStageResponse`/`IMatchResponse`
  directly, never the audit trail).

## 5. Frontend wizard flow, file by file

- `Club12-WebClient/src/views/tournament/wizard/types.ts` — `ZoneConfig`
  (line 140) carries `hasGroupStage: boolean` (default `true` in
  `createEmptyZone`, line 219-227) plus `cups: CupConfig[]`. `CupConfig`
  carries `qualifiers`, `bestOfByStage`, `hasThirdPlace`. `STAGE_TYPE_LABELS`,
  `qualifiersToStageTypes`, `getStageBestOf` are shared helpers for deriving a
  cup's bracket rounds from its qualifier count.
- `wizard/wizardLogic.ts` — pure validation (`validateZonesStep`,
  `validateCrossCupStep`) and the review-step tree builder
  (`buildWizardTree`/`buildGroupAndCupNodes`, line 195-219): when
  `hasGroupStage` is false, `buildGroupAndCupNodes` simply skips the "Fase de
  grupos" tree node and only lists cups — this is the wizard-side reflection of
  "no Group stage gets created," confirming the intended behavior is by
  design, not an oversight; the oversight is downstream (assignment UI can't
  cope with the resulting state).
- `wizard/submitWizard.ts` — `buildZoneDivision` (line 137, body not fully
  read in this pass but referenced/called from `submitWizard`, line 258) turns
  a `ZoneConfig` into an `ICreateFullDivisionRequest`, presumably branching on
  `hasGroupStage` to decide whether to include a Group-type
  `ICreateFullStageRequest`. `submitWizard` posts the whole tournament
  (`POST /api/tournaments/full`) as one atomic transaction via
  `tournamentService.createFullTournament`.
- `views/tournament/wizard/steps/ZoneEditor.tsx`, `DivisionesStep.tsx` — the
  actual checkbox UI for `hasGroupStage` (not read line-by-line this pass;
  `ZoneEditor` renders `CupsEditor` per the codegraph dynamic-dispatch map).
- `views/tournament/TournamentDivisionAssignment.tsx` — the buggy assignment
  workspace itself (HU-108/HU-109), full flow read: loads divisions + enrolled
  teams + per-division Group stages in one `useEffect` (lines 262-341), renders
  a `TeamPickerDialog` (lines 102-213) per assignable group, calls
  `assignTeamsToStage`/`unassignTeamsFromStage` on add/remove
  (`handleAdd`/`handleRemove`, lines 387-415+), and gates the whole workspace
  on `tournament.status` (`canAssign` = `OpenForRegistration` or
  `RegistrationClosed`).

## 6. Test coverage map

**Backend** (all in `Club12-Backend/API.Tests/`), directly relevant to this
change's blast radius:

| Area | Test file(s) |
|---|---|
| `AssignTeamsToStageAsync`/`UnassignTeamsFromStageAsync` | `StageServiceTests.cs`, `StageTeamAssignmentConsistencyTests.cs`, `UnassignedTeamsTests.cs` |
| `SeedKnockoutStageAsync` (group-standings path) | `StageSeedingTests.cs` |
| Multi-group cross-cup seeding | `CrossCupMultiGroupSeedingTests.cs`, `CrossCupMultiGroupBracketGenerationTests.cs`, `CrossCupGroupSeederTests.cs` |
| Playoff-cup seeding from playoff mappings | `StagePlayoffCupSeedingTests.cs` |
| Pure bracket-pairing/bye logic | `PlayoffSeederTests.cs` |
| Division standings/positions | `DivisionGroupStandingsTests.cs`, `DivisionListPositionsTests.cs`, `DivisionStandingsTests.cs` |
| Division↔tournament reassignment, delete integrity | `DivisionTournamentReassignmentTests.cs`, `TeamTournamentDivisionDeleteIntegrityTests.cs` |
| Full-division/full-tournament creation | `TournamentAddFullDivisionTests.cs` |
| Stage CRUD/slug | `StageSlugTests.cs` |

No existing test file targets `EnsureDivisionStructureEditableAsync` directly
(codegraph flags it "⚠️ no covering tests found" as a symbol, though it is
exercised indirectly through the `AssignTeamsToStageAsync`/`CreateStageAsync`
tests above). `SeedKnockoutStageAsync` as a raw symbol is also flagged with no
direct covering test at the controller layer, though `StageSeedingTests.cs`
covers the service method.

**Frontend**: `wizard/wizardLogic.test.ts` and `wizard/submitWizard.test.ts`
cover the wizard-side logic. **`TournamentDivisionAssignment.tsx` has zero
existing test coverage** — codegraph explicitly flags `TeamPickerDialog` with
"⚠️ no covering tests found," and no `TournamentDivisionAssignment.test.tsx`
file exists. This is a concrete gap: proposal/tasks must budget for writing
first-time tests of this component, not just extending existing ones, and
should design the fix so the (currently untested) component becomes testable
in the process.

## 7. Keeping the model open (not building, must not preclude)

The brief asks the roster/seeding model to not actively rule out four future
directions. Notes for the propose/design phase, not decisions:

- **Split-league (round-robin → championship/relegation pools)**: needs a
  division to hold *more than one* sequential "phase" that each re-slot from
  the same roster — `DivisionTeamRegistration` as the durable roster and
  `Stage` as the disposable, rebuildable structural layer (established in
  §3 for HU-123) already generalizes to "rebuild the structural layer twice,
  once per phase," provided nothing hardcodes "a division has at most one
  Group stage" as a business rule rather than a UI assumption. Note:
  `StageService.CreateStageAsync` (line 218-231) *does* currently enforce
  "at most one Group stage per non-cross-cup division" as a hard invariant —
  a split-league division would need to either be modeled as a
  cross-division-cup-like exemption or that invariant would need loosening.
  Worth flagging explicitly, not silently working around.
- **Consolation bracket** (first-round knockout losers get their own bracket):
  `Stage.BracketName` already exists precisely to let multiple *parallel*
  elimination paths coexist under one division (`"Copa de Oro"` /
  `"Copa de Plata"` today, per its own doc comment) — a consolation bracket is
  structurally the same shape (a second bracket seeded from first-round
  losers instead of pre-tournament standings). The seeding *source* differs
  (losers of a specific round vs. a roster/draw), but the bracket-building
  primitives (`PlayoffSeeder.SeedPairs`, `FillStageWithSeedsAsync`) don't care
  where the ordered list came from — no design changes needed there, just a
  new "who feeds the seed list" adapter, analogous to what this change is
  already building for random/manual draw.
- **Repechaje / playoff-in** (near-qualifiers get one extra elimination round
  before joining the main bracket): same shape again — an extra `Stage` whose
  winners become inputs to the next stage's seed list, which
  `TryAdvanceStageWinnerAsync` already supports for the "push a decided slot's
  winner into the next round" mechanic; a repechaje's "winner becomes visitor
  in a slot alongside pre-seeded direct qualifiers" case does not currently
  exist and would need explicit design, but nothing in the entity model
  blocks it.
- **Swiss-system pairing** (tentative, lowest priority): this is the one
  direction that genuinely does not fit the current `Stage`/`Match` shape at
  all — Swiss needs round-by-round dynamic re-pairing based on cumulative
  score, not a pre-generated fixed bracket of `Match` rows. Nothing to design
  now; just avoid baking "every stage's matches are fully known and created
  up front" any deeper into `DivisionTeamRegistration` itself than it already
  is in `Stage`— the roster entity itself (team ↔ division, no stage
  reference) is naturally Swiss-agnostic since it says nothing about pairing
  structure.
- Confirmed **explicitly out of scope**: promotion/relegation across seasons —
  no entity or service in this exploration touches cross-season promotion
  logic; `TeamTournamentRegistration`/`PlayerTeamRegistration` are already
  season-scoped via `TournamentId`, and nothing here needs to change for that.

## 8. Open questions for sdd-propose

1. **Does `DivisionTeamRegistration` replace `StageTeamMatch`'s role, or
   coexist during a migration window?** Two shapes are possible:
   (a) `StageTeamMatch` keeps meaning "assigned to this specific stage/group"
   (its current, narrower meaning) and `DivisionTeamRegistration` becomes a
   new, additional "enrolled in this division at all" fact — teams get
   registered to the division first, then a subset relationship
   (`StageTeamMatch`) places them into specific group-stages/bracket slots.
   (b) `DivisionTeamRegistration` absorbs the "which division" concern
   entirely and `StageTeamMatch` narrows to only mean "which slot within an
   elimination bracket," with group-phase "membership" expressed purely
   through `DivisionTeamRegistration` + a separate sub-group-assignment concept
   (needed anyway for HU-121/122's "N sub-groups within one division").
   Shape (a) is the smaller, additive, lower-risk change; shape (b) is a
   larger refactor that better matches "division-level roster, stage
   attachment is a distinct later step" as literally stated in the brief.
   This is the single highest-leverage decision for the design phase.
2. **Backfill mechanics**: assuming `DivisionTeamRegistration` is added
   additively, existing `StageTeamMatch` rows imply historical
   `(TeamId, DivisionId)` pairs via `Stage.DivisionId`. A migration/backfill
   step (raw SQL `INSERT ... SELECT DISTINCT stm."TeamId", s."DivisionId" ...
   FROM "StageTeamMatches" stm JOIN "Stages" s ON stm."StageId" = s."Id"`,
   following the `RebackfillDivisionStageSlugs`-style data-migration
   precedent) is needed so no existing tournament silently loses roster
   history. Cross-division-cup teams (which can legitimately appear under two
   divisions via two different `StageTeamMatch` rows) must produce two
   `DivisionTeamRegistration` rows, one per division, not be deduplicated away.
3. **Does `DivisionTeamRegistration` need a status/lifecycle field** (mirroring
   `PlayerTeamRegistration`'s `MedicalRecordStatus` pattern), e.g. to
   distinguish "enrolled, not yet slotted into a stage" from "enrolled and
   playing," or is boolean presence in the table sufficient? Affects whether
   `TournamentCompletabilityValidator` (HU-109's extension point mentioned in
   HU-121) reads the roster directly or still infers completeness from
   `Stage`/`StageTeamMatch` counts.
4. **Where does the cross-division-conflict rule
   (`EnsureNoCrossDivisionConflictAsync`) live going forward** — does it move
   to validate against `DivisionTeamRegistration` instead of/alongside
   `StageTeamMatch`, given `DivisionTeamRegistration` is meant to become the
   authoritative "which division(s) is this team in" fact?
5. **Server-side vs. client-side draw preview** (see §4) — affects whether a
   new read-only preview endpoint is needed or the existing pattern (client
   computes locally, as HU-121's group-balance preview already does) extends
   to the playoff draw.
6. **New `AuditAction` member naming** (`PlayoffDraw`? `BracketSeeding`?
   `DivisionSeeding`?) and whether "sorteo realizado el…" is served from a new
   `Stage`/`Division` column or from a public-readable slice of the audit
   trail (see §4) — both are three-file-touching decisions (backend enum +
   frontend type + label map, or backend column + DTO + frontend display)
   that should be pinned down before spec/tasks.
7. **HU-124's fate**: delete `CreateAutomatedStagesAsync` /
   `POST /api/stages/generate/{id}` (dead, UI-orphaned, incompatible fixed-4
   grouping) as part of this change, or explicitly retrofit it to share
   HU-121's balanced-distribution logic? Leaving it untouched risks the
   propose/design phase building a parallel "generate group stages" concept
   under a different name while the old one still lives at a route that
   sounds identical.

## 9. Files most relevant to this change (for propose/design reference)

Backend:
- `Club12-Backend/Domain/Entities/Models/StageTeamMatch.cs`,
  `TeamTournamentRegistration.cs`, `PlayerTeamRegistration.cs`, `Team.cs`,
  `Stage.cs`, `Division.cs` (not fully read this pass — read before design)
- `Club12-Backend/Infrastructure/Persistance/Configurations/StageTeamMatchEntityConfiguration.cs`,
  `TeamTournamentRegistrationEntityConfiguration.cs`,
  `PlayerTeamRegistrationEntityConfiguration.cs`, `BaseEntityConfiguration.cs`
- `Club12-Backend/Application/Services/StageService.cs` (the whole
  assign/unassign/seed surface — `EnsureDivisionStructureEditableAsync`,
  `AssignTeamsToStageAsync`, `UnassignTeamsFromStageAsync`,
  `CreateAutomatedStagesAsync`, `SeedKnockoutStageAsync`,
  `SeedMultiGroupCrossCupStageAsync`, `SeedPlayoffCupsAsync`,
  `FillStageWithSeedsAsync`)
- `Club12-Backend/Application/Utils/Helper/Playoff/PlayoffSeeder.cs`,
  `CrossCupGroupSeeder.cs`
- `Club12-Backend/Application/Services/AuditService.cs`,
  `Domain/Enums/AuditAction.cs`, `Application/Interfaces/Services/IAuditService.cs`
- `Club12-Backend/Infrastructure/Persistance/EntityConstants.cs`,
  `ApplicationDBContext.cs`
- Latest migration for naming precedent:
  `Infrastructure/Migrations/20260902173728_AddTeamShirtTertiaryColor.cs`

Frontend:
- `Club12-WebClient/src/views/tournament/TournamentDivisionAssignment.tsx`
  (the bug site, whole file relevant)
- `Club12-WebClient/src/views/tournament/wizard/types.ts`,
  `wizardLogic.ts`, `submitWizard.ts`,
  `wizard/steps/ZoneEditor.tsx`, `wizard/steps/DivisionesStep.tsx`
- `Club12-WebClient/src/modules/stage/service/stage.service.ts`,
  `modules/stage/type/stage.ts`
- `Club12-WebClient/src/modules/auditLog/type/auditLog.d.ts`,
  `views/panel/AuditLogsPage.tsx`
- `Club12-WebClient/src/modules/playoff/buildBracket.ts`,
  `modules/playoff/type/bracket.d.ts` (bracket rendering, relevant to the
  public "sorteo realizado el…" surfacing)

## 10. Referenced user stories

`Docs/historias-de-usuario.md` lines 738-812: HU-121, HU-122, HU-123 (in
scope), HU-124 (adjacent tech debt, flagged as a decision point above).
