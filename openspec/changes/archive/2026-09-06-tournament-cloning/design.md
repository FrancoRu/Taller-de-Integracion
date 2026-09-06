# Design: Tournament Cloning (wizard-prefill)

## Technical Approach

Additive backend READ endpoint returns a source tournament's pure structure tree
(`TournamentStructureResponse`). A new frontend pure transform reverse-maps that tree into a
`WizardState`, seeds `TournamentWizardPage` via the existing `location.state` mechanism, and the
organizer reviews/edits before submitting through the unchanged `/full` transaction
(`CreateFullTournamentAsync`). No new entities, no migration, no second write path. Zero
instance data (rosters, matches, standings, sanctions, audit, `DrawnAt`) is ever projected — the
DTO simply does not carry it, so it cannot leak.

## Architecture Decisions

### D1 — Reverse-mapper lossiness (THE core risk)
**Choice**: Do NOT reverse `qualifiersToStageTypes` (a lossy range function: Semis+Final ⇒ 3 *or* 4).
Instead read the **authoritative numeric source** for each cup and cross-check it against the
persisted bracket shape; on mismatch raise a visible per-zone/per-cup review notice, never a silent
guess.
- Regular-zone cup qualifiers ⇐ `PlayoffMapping` (`ToPosition − FromPosition + 1`) — exact, since
  `deriveCupMappings` wrote it from the same `cup.qualifiers`.
- Cross-cup pooled total ⇐ `groupCount × Division.QualifiersPerGroup` — exact.
- `bestOfByStage` ⇐ each stage's `BestOf`; `hasThirdPlace` ⇐ presence of a `ThirdPlace` stage.
**Cross-check**: recompute `qualifiersToStageTypes(qualifiers, hasThirdPlace)` and compare to the
distinct stage types actually present. Mismatch (manual edits, drawn/legacy shapes, a regular cup
with bracket stages but no mapping) ⇒ append a notice to `review[]`; fall back to the minimum
qualifier of the bracket's range so the wizard stays valid and editable.
**Rejected**: reversing bracket depth alone (ambiguous), or trusting one source blindly (silent
corruption on hand-edited tournaments).

### D2 — New explicit DTO, additive only
**Choice**: New `TournamentStructureResponse` tree in `Application/DTOs/Tournament/Response/`;
never widen `TournamentResponse`/`DivisionResponse`/`StageResponse` (used by unrelated live pages).
Reuse existing `PlayoffMappingResponse`. **Rejected**: adding `Stages` to `DivisionResponse` — would
change payloads for standings/detail pages.

### D3 — Read endpoint, not a clone write endpoint
**Choice**: `GET /api/tournaments/{idOrSlug}/structure`; all creation stays on the single tested
`/full` transaction. **Rejected**: `POST /clone` deep-copy (duplicates category/deadline/bracket
rules that drift). Matches exploration Approach 1.

### D4 — Reverse-map in the detail-page action, seed wizard via `location.state`
**Choice**: The "Clonar torneo" handler fetches the structure, runs the pure transform, and
navigates with `{ clonePrefill: WizardState, cloneReview: string[] }`. Wizard consumes `clonePrefill`
as its initial state and renders `cloneReview` as a persistent banner. **Rejected**: fetching +
mapping inside the wizard's mount (spreads network/loading into a pure UI shell).

### D5 — Category explicit, dates & season blank
**Choice**: `category` comes from an organizer dialog on the action, threaded onto the prefilled
`WizardState`, never inherited from the source. `startDate`/`teamRegistrationDeadline` prefill blank
(required fields ⇒ wizard blocks submit until entered). `seasonId` blank ⇒ organizer picks the target
season in step 1 (existing standalone-launch path, `seasonPreset=false`).

## Data Flow

    TournamentPage "Clonar torneo" ─(category dialog)─► useTournament.getStructure(idOrSlug)
         │                                                        │
         │                                             GET /{idOrSlug}/structure
         │                                                        ▼
         │                            TournamentService.GetTournamentStructureAsync
         │                            (Tournament ⊃ Divisions ⊃ Stages, PlayoffMappings)
         │                                                        │  AutoMapper
         │                                             TournamentStructureResponse
         ▼                                                        │
    structureToWizardState(dto, category) ──► { state: WizardState, review: string[] }
         │
    navigate(panelTournamentWizard, { state: { clonePrefill, cloneReview } })
         ▼
    TournamentWizardPage (initial state = clonePrefill, banner = cloneReview)
         └──► existing submitWizard ──► POST /api/tournaments/full (UNCHANGED)

## Reverse-mapping rules (per division ⇒ `ZoneConfig` | `CrossCupConfig`)

`groupStages = stages[StageType==Group]`, `elimStages = stages[BracketName != null]`.

| Target field | Source rule |
|---|---|
| zone `name` | `Division.Name` |
| `hasGroupStage` | `groupStages.length ≥ 1` |
| `subGroupCount` (regular) | `groupStages.length` (1 ⇒ "Fase de Grupos"; N ⇒ "Grupo A".."Grupo N") |
| `groupCount` (cross) | `groupStages.length` ("Grupo 1".."Grupo N") |
| `qualifiersPerGroup` (cross) | `Division.QualifiersPerGroup` |
| `roundRobinLegs` | any group stage's `RoundRobinLegs`; playoffs-only ⇒ default 1 |
| `pointsForWin/Loss` | `Division.PointsForWin/PointsForLoss` |
| cups[] | group `elimStages` by `BracketName`; one `CupConfig` per bracket |
| cup `name` | `BracketName` |
| cup `qualifiers` (regular) | matching `PlayoffMapping` span; order cups by `FromPosition` |
| cup `qualifiers` (cross) | `groupCount × qualifiersPerGroup`; order by `Stage.Order` |
| `hasThirdPlace` | `ThirdPlace` stage present in bracket |
| `bestOfByStage` | `{ stageType: BestOf }` per bracket stage |

`IsCrossDivisionCup` selects the `CrossCupConfig` branch (there is at most one such division ⇒ the
single `crossCup`); all others map to `zones[]`. Guard
`SubGroupsIncompatibleWithPositionRangeCups` guarantees no division mixes `subGroupCount ≥ 2` with
position-range cups, so the two branches never collide.

## File Changes

| File | Action | Description |
|---|---|---|
| `Application/DTOs/Tournament/Response/TournamentStructureResponse.cs` | Create | Tree: divisions ⊃ `DivisionStructureResponse` ⊃ `StageStructureResponse` + reused `PlayoffMappingResponse` |
| `Application/Services/TournamentService.cs` + `ITournamentService.cs` | Modify | `GetTournamentStructureAsync(Guid)` — loads graph with `Divisions.Stages`, `Divisions.PlayoffMappings` includes |
| existing AutoMapper profile | Modify | `Tournament→TournamentStructureResponse`, `Division→DivisionStructureResponse`, `Stage→StageStructureResponse` |
| `API/Controllers/TournamentController.cs` | Modify | `GET {idOrSlug}/structure` ⇒ 200 DTO / 404 |
| `WebClient/.../wizard/cloneWizard.ts` | Create | `structureToWizardState(dto, category)` — pure, sibling of `submitWizard.ts` |
| `WebClient/.../wizard/TournamentWizardPage.tsx` | Modify | Read `clonePrefill`/`cloneReview` from `location.state`; keep `{ seasonId }`-only path working |
| `WebClient/.../tournament/TournamentPage.tsx` | Modify | "Clonar torneo" action (gated `canEditTournament`) + category dialog → fetch/map/navigate |
| `modules/tournament/hook/tournament.hook` + service/type | Modify | `getStructure(idOrSlug)` + `ITournamentStructureResponse` types |

## Interfaces / Contracts

```csharp
public class TournamentStructureResponse {
    public required string Name; public string? Description;
    public required TournamentCategory Category;               // shown for reference; NOT auto-applied
    public required List<DivisionStructureResponse> Divisions;
}
public class DivisionStructureResponse {
    public required string Name; public bool IsCrossDivisionCup;
    public int PointsForWin, PointsForLoss, QualifiersPerGroup;
    public List<PlayoffMappingResponse> PlayoffMappings = [];
    public required List<StageStructureResponse> Stages;       // NO DrawnAt/matches/roster
}
public class StageStructureResponse {
    public required string Name; public string? BracketName;
    public required StageType StageType; public bool IsElimination;
    public int Order, BestOf, RoundRobinLegs;
}
```

```ts
// cloneWizard.ts
export const structureToWizardState = (
  dto: ITournamentStructureResponse, category: TournamentCategory
): { state: WizardState; review: string[] };
// location.state widened, both keys optional (backward compatible):
type WizardLocationState = { seasonId?: string; clonePrefill?: WizardState; cloneReview?: string[] };
```

## Testing Strategy

| Layer | What | Approach |
|---|---|---|
| Unit (BE) | `GetTournamentStructureAsync` includes graph, projects zero instance data | service test |
| Unit (FE) | `structureToWizardState` round-trips: multi-division/multi-substage, cross-cup, playoffs-only, blank dates, mismatch⇒`review[]` | pure-function tests beside `submitWizard.test` |
| Round-trip | `submitWizard(structureToWizardState(GET structure)) ≈ source structure` | golden fixtures |
| Integration | detail action → wizard prefilled, editable-before-submit changes final `/full` payload, empty rosters | RTL + API |

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or
process-integration boundary. New surface is one authenticated additive READ endpoint.

## Migration / Rollout

No migration required. Additive; rollback = revert the endpoint/DTO + `cloneWizard.ts` + detail
action; wizard and `/full` transaction untouched.

## Open Questions

- [ ] Prefill new tournament `name` as source name verbatim vs. suffixed (e.g. "(copia)")? Leaning
  verbatim, organizer edits. Non-blocking.
