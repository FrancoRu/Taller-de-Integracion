# Tasks: Statistics Page Filter UX

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~30 source + ~50 test |
| 400-line budget risk | None |
| Chained PRs recommended | No — bundled with the season/club-history fixes |
| Delivery strategy | single PR |

## Phase 1: Frontend RED — `StatisticsPage.test.tsx`

- [x] 1.1 RED: after load, the "Temporada" combobox reads "Todas" and "Torneo" reads "Todos".
- [x] 1.2 RED: with a deferred summary fetch, selecting a tournament leaves both comboboxes mounted (filter bar not replaced by the skeleton).

## Phase 2: Frontend GREEN — `StatisticsPage.tsx`

- [x] 2.1 `slotProps={{ select: { displayEmpty: true }, inputLabel: { shrink: true } }}` on both `<TextField select>`.
- [x] 2.2 Extracted a `filterBar` const used in both returns; `if (!summary)` (was `loading || !summary`) renders `{filterBar}` + `CardGridSkeleton`; the main return renders `{filterBar}` then `{loading && <LinearProgress sx={{ mb: 1 }} />}` then the grids unchanged.
- [x] 2.3 4 `StatisticsPage` tests green.

## Phase 3: Regression

- [x] 3.1 `StatisticsPage.test.tsx` — 4/4.
- [x] 3.2 `npx tsc --noEmit` exit 0; `npm run lint` exit 0. Full `vitest run`: same unrelated `VenuesPage` flake under parallel load (passes isolated 2/2); no venue files touched.

## Phase 4: Manual dev-DB verification (pending — owner login)

- [ ] 4.1 `/panel/estadisticas`: both selects show "Todas"/"Todos"; changing a filter updates only the cards (filter bar stays, dropdown does not collapse the layout).
