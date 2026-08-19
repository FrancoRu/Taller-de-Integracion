# Proposal: Playoff Bracket Visualizer & Print-Friendly Standings

## Intent

The public tournament view renders the elimination stage (cuartos, semifinal, tercer puesto, final) as a flat `MatchCard` grid, so visitors cannot read the knockout path at a glance. Organizers also have no clean way to hand out or post standings/goleadores. Deliver two public-facing, robustness-first frontend features: a real bracket tree and a printable standings/scorers view.

## Scope

### In Scope
- Bracket-tree component for elimination stages on `PublicTournamentPage` (per division), driven by existing Stage + Match GET responses. Rounds, winners, scores, TBD slots, and connectors, aligned to the modern-sport theme.
- Client-side bracket model builder: rounds from `StageType` order; matches grouped by `stageId`; connectors inferred by winner→next-round team propagation. `ThirdPlace` rendered as a separate side match.
- Print-friendly view for `DivisionStandings` (and goleadores) via `@media print` + `window.print()`, with a print-optimized layout and a "Imprimir / PDF" action.
- Empty/partial/unplayed-match states; multi-division handling; unit tests for the bracket builder.

### Out of Scope
- Any backend change or new endpoint (data already sufficient — see Approach).
- Bracket editing/seeding, drag-drop, live updates (SignalR), audit log, email — explicitly deferred.
- Group-stage bracketing (knockout stages only).

## Capabilities

### New Capabilities
- `playoff-bracket`: public client-side bracket-tree rendering of elimination stages from existing Stage/Match data.
- `printable-standings`: print/PDF-friendly standings and goleadores output for organizers.

### Modified Capabilities
- None.

## Approach

Match entity exposes only `homeTeam`/`visitorTeam`/`winningTeamId`/`stageId` (verified in backend + `match.d.ts`); no `round`/`nextMatchId`/parent field exists. So build the tree in a new `modules/playoff` builder: order elimination stages by canonical `StageType` sequence, group matches per stage, and derive round-to-round edges by matching a match's `winningTeamId` to a participant in the next round. This needs elimination stages (`getStagesByFilters` with `isElimination`) plus matches — both already served. Render with MUI `styled`/`sx` (the codebase's styled-components-backed engine) and a scoped CSS/flex column layout; no new styling system.

Print: recommend native `@media print` + `window.print()` (browser print-to-PDF) over `jspdf`/`html2canvas` (rasterized, blurry text, heavy bundle) or `react-to-print` (thin, optional). Native gives crisp tables, zero heavy deps, best fidelity.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/modules/playoff/*` | New | Bracket model builder + types |
| `src/views/home/tournaments/PublicTournamentPage.tsx` | Modified | Bracket tab/section |
| `src/views/**/PlayoffBracket*` | New | Bracket tree + round/match nodes |
| `src/views/division/divisionStandings.tsx` | Modified | Print action + print styles |
| `src/theme.ts` | Read only | Reuse existing tokens |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Connector inference wrong when teams repeat / matches unplayed | Med | Fall back to column layout without edges; show TBD; test builder |
| ThirdPlace/Final placement ambiguity | Med | Treat ThirdPlace as side match, Final as terminal node |
| Long standings tables paginate poorly in print | Low | Print CSS: page-break rules, hide chrome |

## Rollback Plan

Both features are additive. Revert the feature branch: remove `modules/playoff` and new view components, and restore `PublicTournamentPage`/`divisionStandings` to prior revisions. No data/schema/API changes to undo.

## Dependencies

- Existing Stage/Match/Division GET endpoints and hooks. No new packages required.

## Success Criteria

- [ ] Elimination stages render as a readable bracket tree per division on the public view.
- [ ] Winners, scores, and TBD/unplayed slots display correctly; connectors reflect advancement or degrade gracefully.
- [ ] Standings/goleadores print cleanly to paper/PDF with only the table visible.
- [ ] No backend changes; no new heavy dependencies; feature-module + MUI conventions followed.

## Proposal question round

Sub-agent cannot prompt interactively; these assumptions need user review before spec/design:
1. Is the bracket scoped per division (one tree per division inside a tournament), matching how standings already render? (Assumed yes.)
2. Should the bracket be a new tab on `PublicTournamentPage` or replace the current "Partidos" elimination view? (Assumed new "Llaves" tab, keeping Partidos.)
3. Is winner→next-round team-propagation an acceptable way to draw connectors given no backend linkage, with graceful degradation to a connector-less column layout when inference is ambiguous? (Assumed yes.)
4. Print target — standings only, or standings + goleadores in one printable sheet? (Assumed both, selectable.)
5. Confirm native browser print (no PDF library) is acceptable vs. a generated downloadable PDF file. (Assumed native print.)
