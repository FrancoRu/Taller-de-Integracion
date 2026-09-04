# Proposal: Champions Page — Seasons Newest-First in Collapsible Accordions

**Touches**: Backend (`ChampionHistoryResponse` + `ChampionService`) and
frontend (`groupChampions`, `PublicChampionsPage`, `champion.d.ts`). No schema,
no migration.

## Intent

`/campeones` renders the right hierarchy already — **Season → Tournament (with
its category badge) → Division → per-cup champion cards**. Two things are off:

1. **Seasons are not ordered.** `groupChampions` keeps the backend's
   first-appearance order, and `ChampionService.GetChampionsHistoryAsync`
   fetches finished tournaments with no ordering (Postgres heap order). So
   "Temporada 2025" can render above "Temporada 2026". The organizer expects
   the most recent season first.

2. **Every season is always fully expanded.** With several seasons the page
   is a very long scroll. Each season should be a collapsible accordion, with
   only the most recent one open by default.

## Scope

### In Scope

- `ChampionHistoryResponse` gains `SeasonYear` (`int?`, from
  `Tournament.Season?.Year`).
- `groupChampions` sorts the season buckets by year descending; seasons with
  no year (including the "Sin temporada" bucket) sort last, `seasonName`
  descending as the deterministic tiebreak. Each `ChampionSeasonGroup`
  carries its `seasonYear`.
- `PublicChampionsPage` wraps each season in a MUI `<Accordion>`;
  `defaultExpanded` is true only for the first (newest) season.
- `IChampionHistory` gains `seasonYear`.
- Backend + frontend tests.

### Out of Scope (Non-Goals)

- Reordering tournaments or divisions within a season — they keep the
  backend's first-appearance order (already the tested contract).
- Persisting the expanded/collapsed state per visitor.
- Any change to the champion **resolution** logic (`ChampionResolver`, cup
  tiers, podium).
- Scoping / a season filter on the page.

## Capabilities

### New Capabilities

- `champions-history-view`: the ordering of seasons on `/campeones` and their
  collapsible presentation.

### Modified Capabilities

- None.

## Approach

Add the one missing sort key (`Season.Year`) to the champion-history
projection — additive, the response is hand-built and has no AutoMapper map,
so no consumer breaks. Sort the season buckets in `groupChampions` (numbers
desc, nulls last, name desc tiebreak). Presentation-only accordion in the
page, uncontrolled (`defaultExpanded` on index 0).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/DTOs/Champions/Response/ChampionHistoryResponse.cs` | Modified | `SeasonYear` added |
| `Application/Services/ChampionService.cs` (`GetChampionsHistoryAsync`) | Modified | `SeasonYear = tournament.Season?.Year` |
| `Club12-WebClient/src/modules/champion/type/champion.d.ts` | Modified | `IChampionHistory.seasonYear` |
| `Club12-WebClient/src/modules/champion/utils/groupChampions.ts` | Modified | sort seasons desc by year; carry `seasonYear` |
| `Club12-WebClient/src/views/home/champions/PublicChampionsPage.tsx` | Modified | `<Accordion>` per season, newest `defaultExpanded` |
| `API.Tests/ChampionServiceTests.cs` | Modified | `SeasonYear` populated / null |
| `groupChampions.test.ts` | Modified | first-seen order → year-desc order |
| `PublicChampionsPage.test.tsx` | Modified | newest accordion open, older collapsed |

`gitnexus impact` on `GetChampionsHistoryAsync` reports MEDIUM (controller +
tests), but the change is an additive response field — the method signature
and every caller are untouched.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| A season exists but `Year` is null | Expected | Sorts with the "Sin temporada" tail, `seasonName` desc keeps it deterministic |
| Test asserts the old first-seen season order | Certain | `groupChampions.test.ts` "preserves the first-seen order" is rewritten to assert year-desc |
| Accordion hides content from crawlers / deep links | Low | Public SEO copy is in `usePageMetadata`; the DOM still renders collapsed panels (MUI keeps them mounted unless `TransitionProps={{ unmountOnExit }}`, which we do not set) |
| `getByRole('heading', { name })` in the page test breaks | Low | Season name stays a real heading inside `AccordionSummary` |

## Rollback Plan

Revert the commit. `SeasonYear` is additive; the page falls back to
first-appearance order and flat sections. No data or schema state.

## Success Criteria

- [ ] `GetChampionsHistoryAsync` sets `SeasonYear` from the tournament's
      season (null when there is no season or the season has no year).
- [ ] `groupChampions` returns seasons ordered by year descending, null-year
      seasons last.
- [ ] `/campeones` shows the newest season's accordion expanded and the rest
      collapsed.
- [ ] Backend + frontend suites green; `dotnet build` 0 warnings; `tsc` + lint clean.
