# Exploration: tournament-cloning

## Current State

**Data model** (`Club12-Backend/Domain/Entities/Models/`):
- `Tournament` — Name, Description, Slug, StartDate, TeamRegistrationDeadline, `Status` (forward-only
  state machine), `Category` (enum `Masculine`/`Feminine`, immutable after create, drives a no-mixing
  invariant across its divisions — feminine is *by design a separate tournament*, confirmed via
  `TournamentCategoryTests.cs`), optional `SeasonId`/`Season` (**one Season has many Tournaments; one
  Tournament belongs to 0 or 1 Season** — nullable FK), `Divisions`, `Teams`.
- `Division` — Name, Slug, `TournamentId`, `Category` (must match parent Tournament),
  `IsCrossDivisionCup`, `PointsForWin`/`PointsForLoss` (default 2/1), `QualifiersPerGroup`,
  `PlayoffMappings`, `DivisionTeamRegistrations`, `Stages`.
- `Stage` — Name, Slug, `StageType`, `IsElimination`, dates, `Order`, `BracketName`, `BestOf`,
  `RoundRobinLegs`, `DrawnAt`.
- `DivisionPlayoffMapping` — `DivisionId`, `FromPosition`/`ToPosition`, `Destination` string.
- `DivisionTeamRegistration` (added this session) — just `TeamId`+`DivisionId`, the roster join.
- `Team` — per-season/per-tournament *instance* (kit fields, `TournamentId`, optional
  `ClubId`/`Club`). Confirmed the prior memory note is accurate: **`Club` is the stable cross-season
  entity** ("`Club.cs` docstring: persists across seasons, unlike a Team, which is a per-season
  registration record"); `Team` is season/tournament-scoped.
- `TournamentCategory` is a plain 2-value enum, not a separate entity — nothing extra to clone beyond
  copying the value.

**No clone/duplicate/template capability exists anywhere** — confirmed
`ITournamentService`/`TournamentService.cs` has only `CreateTournamentAsync`,
`CreateFullTournamentAsync`, `AddFullDivisionAsync`, `GetTournamentByIdAsync`/
`GetTournamentByIdOrSlugAsync`, `UpdateTournamentAsync`, `ChangeStatusAsync`,
`GetCompletabilityAsync`, `DeleteTournamentAsync`, `GetAllTournamentsAsync`.

**The wizard's own model is already a clean "structure" representation.**
`Club12-WebClient/src/views/tournament/wizard/types.ts` defines
`WizardState { tournament, zones: ZoneConfig[], crossCup }` — pure structure, zero team/roster/match
data. `submitWizard.ts` is a **one-way builder** (`WizardState → ICreateFullTournamentRequest` →
`POST /api/tournaments/full` → `CreateFullTournamentAsync`, one atomic transaction). **No reverse
mapper exists** (Tournament entity graph → WizardState) — would need to be built new.

**The wizard already supports external pre-seeding.** `TournamentWizardPage.tsx` reads
`location.state` and currently consumes only `{ seasonId }`, seeded by `AdminSeasonDetailPage.tsx`
(`navigate(APP_ROUTES.panelTournamentWizard, { state: { seasonId } })`). This is an established, tested
precedent for extending the wizard to accept a fuller pre-populated state.

**GET responses cannot currently reverse-map.** `TournamentResponse.Divisions` is
`IEnumerable<MinimalDivisionResponse>` (just Id/Name/IsFinished). `DivisionResponse` has
points/QualifiersPerGroup/PlayoffMappings/Category but **no Stages**. `StageResponse` is its own
separate DTO. Nothing today assembles a full "divisions + their stages + playoff mappings" tree for one
tournament — any cloning approach needs this new read path.

**Season/Team scoping confirms the task's intuition**: since `Team` (not `Club`) is
season/tournament-scoped, and `DivisionTeamRegistration`/`TeamTournamentRegistration`/
`StageTeamMatch`/matches all key off ids that won't exist in the new tournament, **none of the instance
data can carry over mechanically** — a clone naturally starts with empty rosters per new division,
matching "organizers re-enroll via the roster feature." No code/test implies otherwise. Note:
`RosterCopyTests.cs`/`IRosterCopyService.cs`/`RosterController.cs` exist for *player* roster copying
between teams — a related but distinct concept; worth avoiding a naming collision when this change names
its own service/DTOs.

## Affected Areas

- `Club12-Backend/Domain/Entities/Models/{Tournament,Division,Stage,DivisionPlayoffMapping}.cs` —
  reference only.
- `Club12-Backend/Application/Services/TournamentService.cs`,
  `Application/Interfaces/Services/ITournamentService.cs` — new clone logic lands here.
- `Club12-Backend/Application/DTOs/Tournament/Response/TournamentResponse.cs`,
  `Application/DTOs/Divisions/Response/{DivisionResponse,MinimalDivisionResponse}.cs`,
  `Application/DTOs/Stage/Response/StageResponse.cs` — none assembles a structure tree; new DTO needed
  regardless of approach.
- `Club12-Backend/API/Controllers/TournamentController.cs` — new endpoint (structure-read or
  clone-write).
- `Club12-WebClient/src/views/tournament/wizard/{types.ts,wizardLogic.ts,submitWizard.ts,TournamentWizardPage.tsx}`
  — wizard-prefill approach touches all.
- `Club12-WebClient/src/views/season/AdminSeasonDetailPage.tsx` — natural entry point (already launches
  the wizard pre-scoped by season).
- Test precedent: `Club12-Backend/API.Tests/{FullTournamentCreationTests.cs,
  TournamentAddFullDivisionTests.cs,SeasonTournamentLinkTests.cs}`,
  `Club12-WebClient/src/views/tournament/wizard/*.test.ts(x)`.

## Approaches

1. **Wizard-prefill** (hydrate `WizardState` from a new backend structure-tree endpoint, reuse existing
   `/full` submit path)
   - Pros: zero drift risk (single write path, already tested); organizer reviews/edits before
     committing; extends an already-proven pre-seeding pattern.
   - Cons: needs a brand-new, nontrivial reverse-mapper (Tournament graph → WizardState) with no
     precedent — e.g. reconstructing `subGroupCount`/`CupConfig[]`/`bestOfByStage` from a flat Stage
     list is lossy/heuristic. Two full wizard passes may feel heavy for "just repeat last season."
   - Effort: Medium-High.

2. **Dedicated clone endpoint** (`POST /api/tournaments/{id}/clone`, deep-copies entities directly)
   - Pros: one-click, no wizard pass needed.
   - Cons: bypasses wizard validation/construction entirely — a second creation path (category-match
     rule, deadline-before-start check, bracket-shape derivation) that can silently drift from
     `CreateFullTournamentAsync` over time; no shared "build structure" primitive exists today to
     prevent that.
   - Effort: Medium, but higher long-term risk.

3. **Hybrid** — refactor `CreateFullTournamentAsync`'s internals into a source-agnostic
   structure-builder reused by both a one-click clone AND wizard-prefill.
   - Pros: best of both, no drift.
   - Cons: highest effort; needs approach 1's reverse-mapper anyway.
   - Effort: High.

## Recommendation

**Approach 1 (wizard-prefill)**, with a new explicit `TournamentStructureResponse` DTO (not widening
the load-bearing `TournamentResponse`/`DivisionResponse`). Keeps all tournament creation on the single
tested transaction, matches the existing `location.state` precedent, and gives the organizer a
review/edit step appropriate for a once-a-year, high-stakes operation. Approach 2 is not recommended
first — it duplicates exactly the rules most likely to change. Approach 3 can be revisited later if a
true one-click need emerges.

## Open Questions (need user decision before sdd-propose)

1. Exact scope of "structure": confirmed IN — division names/category/`IsCrossDivisionCup`/points/
   `QualifiersPerGroup`/every Stage's type-order-BracketName-BestOf-RoundRobinLegs/PlayoffMappings;
   confirmed OUT — rosters/teams/matches/standings/sanctions/audit logs. Should dates come over blank,
   or auto-shifted +1 year as a suggestion?
2. Can the source tournament be from the *same* season, or must it always be a different (prior)
   season? Nothing in the data model forces a difference.
3. Selective (checkbox which divisions/cups) vs. full clone-then-edit-in-wizard?
4. Where does "Clonar torneo" live — a row action on `AdminSeasonDetailPage`, a tournament detail
   action, or a source-tournament picker inside the wizard's first step?
5. `TeamRegistrationDeadline`/`StartDate` are required and check-constrained (deadline < start) — blank
   on clone, or shifted?

## Risks

- No existing reverse-mapping precedent; the Tournament→WizardState mapper needs new, careful tests
  (subGroupCount/cup/bestOf reconstruction is not mechanically reversible from a flat Stage list).
- `TournamentResponse`/`DivisionResponse` are used by unrelated live pages — any DTO change for this
  feature must be additive-only.
- Wizard hard-requires `seasonId` at validation even though the entity allows null — a clone flow needs
  its own season-selection step regardless of chosen approach.
- Category is immutable and tournament-wide — cross-category cloning is structurally possible but the
  target category must be an explicit organizer choice, never silently inherited.

## Ready for Proposal

No — the five open questions above must be resolved first.
