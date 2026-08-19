# Design: Playoff Bracket Visualizer & Print-Friendly Standings

## Technical Approach

Two additive, frontend-only features on the public tournament view, built entirely
from existing GET data (Stage + Match + Division). A pure `modules/playoff` builder
transforms flat `IStageResponse[]` + `IMatchResponse[]` into an ordered bracket model
consumed by MUI-styled tree components. Print uses native `@media print` +
`window.print()` — zero new dependencies. Settled per user decisions: new "Llaves"
tab (keep "Partidos"); client-side connector inference WITH graceful degradation;
print sheet covers standings + goleadores (selectable); native browser print.

## Verified Data Contracts (real, on-disk)

- `IMatchResponse`: `id`, `stageId`, `homeTeam`/`visitorTeam: ITeamMatchResponse | null`
  (`id`,`name`,`logoUrl`,`score`), `isFinished`, `winningTeamId: GUID | null`.
  **No round/nextMatchId/parent field** — edges must be inferred.
- `IStageResponse`: `stageType: StageType`, `order: number`, `isElimination`, `divisionId`.
- `StageType` enum: `Group | QuarterFinal | SemiFinal | ThirdPlace | Final`.
- `DivisionTopScoreResponse`: `playerid`,`firstName`,`lastName`,`totalPoints` via
  `useDivision().getTopScoresByDivisionId(id)`.
- Hooks: `useStage().getStagesByFilters({divisionId, isElimination:true})`,
  `useMatch().getMatchByFilter({divisionId})`.

## Architecture Decisions

| Decision | Choice | Rejected | Rationale |
|---|---|---|---|
| Bracket model source | Pure builder in `modules/playoff` from Stage+Match | Component-local grouping | Testable pure fn; no backend change |
| Round ordering | `ROUND_ORDER` map on StageType (QF=1,SF=2,Final=3), tie-break `stage.order` | Sort by `order` alone | `order` not guaranteed cross-type; enum is canonical |
| ThirdPlace | Side node rendered next to Final, not its own column | Own column | Matches real bracket layout; Final is terminal |
| Connector inference | Match `winningTeamId` to a participant in next round | nextMatchId (absent) | Only signal available |
| Ambiguity handling | Degrade to column-only (no line), never guess | Best-guess line | User-mandated; correctness over decoration |
| Styling | MUI `sx`/`styled`, theme tokens | Tailwind/CSS-Modules | Matches repo convention (styled-engine-sc) |
| Print | `@media print` + `window.print()` | jspdf/html2canvas | Crisp text, no heavy dep |
| Connectors render | SVG overlay only when unambiguous | CSS pseudo-lines | Precise, easy to omit on ambiguity |

## Bracket Builder Logic

`buildBracket(stages, matches): BracketModel`
1. Filter `stages` to `isElimination === true`; drop Group.
2. Partition: main rounds = QuarterFinal/SemiFinal/Final; ThirdPlace held aside.
3. Order main rounds by `ROUND_ORDER[stageType]`, tie-break `stage.order`. Each round =
   `{ stageType, stageId, matches: matches.filter(m => m.stageId === stage.id) }`.
4. Attach ThirdPlace stage/matches to model as `thirdPlace` (side slot beside Final).
5. Compute connector edges: for each source match with `winningTeamId = W` in round N,
   find matches in round N+1 whose `homeTeam.id === W || visitorTeam.id === W`.
   - Emit edge only when **exactly one** target matches.

### Ambiguity → column-only degradation (per edge)
Emit NO connector (render clean columns, TBD slots) when any hold:
- source `winningTeamId` is null (unplayed / draw / no winner yet), OR
- winner W matches **zero** participants in next round (not yet seeded / data gap), OR
- winner W matches **>1** slot in next round (team repeats — data tie), OR
- next round has no matches.
Missing team on a slot renders "A definir" (TBD). Builder always returns a valid model;
`edges` may be empty — the view degrades gracefully.

## Data Flow

    PublicTournamentPage ("Llaves" tab)
      getDivisionsByFilters ─→ per division:
        getStagesByFilters({divisionId, isElimination:true}) ┐
        getMatchByFilter({divisionId})                       ┘→ buildBracket() → BracketModel
      → <PlayoffBracket model /> → RoundColumn[] + MatchNode[] + <BracketConnectors edges/> (SVG)

## File Changes

| File | Action | Description |
|---|---|---|
| `src/modules/playoff/type/bracket.d.ts` | Create | `BracketModel`, `BracketRound`, `BracketEdge` types |
| `src/modules/playoff/buildBracket.ts` | Create | Pure builder + `ROUND_ORDER` |
| `src/modules/playoff/buildBracket.test.ts` | Create | Vitest unit suite |
| `src/views/playoff/PlayoffBracket.tsx` | Create | Per-division tree container, fetch orchestration hook usage |
| `src/views/playoff/BracketMatchNode.tsx` | Create | Match node (teams, scores, winner highlight, TBD) |
| `src/views/playoff/BracketConnectors.tsx` | Create | SVG connector overlay, rendered only from `edges` |
| `src/views/home/tournaments/PublicTournamentPage.tsx` | Modify | Add `'llaves'` tab + Tab type + fetch effect |
| `src/views/division/PrintableResultsSheet.tsx` | Create | Standings+goleadores wrapper, toggle, print button, print CSS |
| `src/views/division/divisionStandings.tsx` | Modify | Accept print-mode props; page-break-safe rows |

## Interfaces

```ts
export interface BracketEdge { fromMatchId: GUID; toMatchId: GUID; }
export interface BracketRound { stageId: GUID; stageType: StageType; matches: IMatchResponse[]; }
export interface BracketModel {
  rounds: BracketRound[];          // ordered QF→SF→Final
  thirdPlace?: BracketRound;       // side match
  edges: BracketEdge[];            // only unambiguous connectors
}
export function buildBracket(stages: IStageResponse[], matches: IMatchResponse[]): BracketModel;
```

## Print CSS Approach (`PrintableResultsSheet`)

- `window.print()` on "Imprimir / PDF" button.
- Toggle state `view: 'standings' | 'goleadores' | 'both'` controls rendered tables.
- MUI `GlobalStyles` with `@media print`: hide chrome via `[data-print="hide"] { display:none }`
  applied to nav/sidebar/tabs/buttons; show only `[data-print="sheet"]`.
- Page breaks: `& tr { break-inside: avoid }`, `& thead { display: table-header-group }`
  (repeats header per page), division blocks `break-inside: avoid`.
- Colors: force `print-color-adjust: exact` so navy header keeps contrast.

## Testing Strategy

| Layer | What | Approach |
|---|---|---|
| Unit | `buildBracket` ordering | QF/SF/Final ordered; ThirdPlace sided; Group dropped |
| Unit | Edge inference | single winner→one next slot emits edge |
| Unit | Degradation | null winner, zero-match, >1-match, empty next round → no edge, valid model |
| Unit | Empty/partial | no elimination stages → empty model; unplayed rounds → TBD, no edges |
| Component | Node/print (optional) | RTL smoke: winner highlight, TBD slot, print toggle renders selected table |

Vitest already configured (`*.test.ts`, `describe/it/expect`); builder is a pure fn — no mocks.

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary. Frontend rendering only.

## Migration / Rollout

No migration. Fully additive; revert feature branch to remove `modules/playoff` + new
views and restore two modified files. No data/schema/API change.

## Open Questions

None — all five proposal questions resolved by user.
