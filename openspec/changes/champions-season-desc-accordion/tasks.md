# Tasks: Champions Page — Seasons Newest-First in Collapsible Accordions

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~25 source + ~120 test, backend + frontend |
| 400-line budget risk | None |
| Chained PRs recommended | No |
| Delivery strategy | single PR |

## Phase 0: Unblock the test harness (pre-existing develop breakage)

- [x] 0.1 `develop`'s `appsettings.json` now ships `Seed:Enabled=true`, so the test host's `ExecuteMigrationsAndSeedAsync` runs the sample seed, which constructs `SupabaseHelper` — its Supabase client ctor throws `Regex.Match(null)` on the null `ProjectUrl` the test host has no config for. Every `WebApplicationFactory` test was failing. Fix: `CustomWebApplicationFactory` sets `Seed__Enabled=false` (env var beats appsettings.json), matching how it already stubs ConnectionStrings/JWT/Smtp. Restores the pre-merge behavior (old `appsettings.json` had `Seed:Enabled=false`).

## Phase 1: Backend RED (strict TDD) — `API.Tests/ChampionServiceTests.cs`

- [x] 1.1 `SeedSeasonAsync(db, year)` helper + `SeedTournamentAsync(..., Guid? seasonId = null)`.
- [x] 1.2 RED `GetChampionsHistoryAsync_CarriesSeasonYear` (compile-fail then value-fail): season `Year = 2026` → entry `SeasonYear == 2026`; no season → `null`.

## Phase 2: Backend GREEN

- [x] 2.1 `ChampionHistoryResponse.SeasonYear` (`int?`).
- [x] 2.2 `ChampionService.GetChampionsHistoryAsync`: `SeasonYear = tournament.Season?.Year`.
- [x] 2.3 10 `ChampionServiceTests` green; full backend suite **835 passed** (was fully red on develop); `dotnet build` 0 warnings.

## Phase 3: Frontend RED

- [x] 3.1 `seasonYear` added to both `entry(...)` factories.
- [x] 3.2 RED `groupChampions.test.ts`: "preserves first-seen order" → "orders seasons by year, newest first"; + "sorts null-year seasons last".
- [x] 3.3 RED `PublicChampionsPage.test.tsx`: two seasons — newest button `aria-expanded=true`, older `false`; clicking the older reveals its tournament.

## Phase 4: Frontend GREEN

- [x] 4.1 `champion.d.ts`: `IChampionHistory.seasonYear: number | null`.
- [x] 4.2 `groupChampions.ts`: `ChampionSeasonGroup.seasonYear`; `.sort` — year desc, null year last, `seasonName` desc tiebreak.
- [x] 4.3 `PublicChampionsPage.tsx`: each season a `<Accordion defaultExpanded={seasonIndex === 0}>` with `slotProps={{ heading: { component: 'h2' } }}` (season name stays a real heading), gold accent bar in the summary, tournaments block in `<AccordionDetails>`. Dropped the `SectionHeading` import.
- [x] 4.4 14 champions frontend tests green.

## Phase 5: Full regression

- [x] 5.1 `dotnet test` — 835 passed / 0 failed.
- [x] 5.2 `dotnet build` — 0 warnings / 0 errors.
- [x] 5.3 `npx tsc --noEmit` exit 0; `npm run lint` exit 0; champions suites green. Full `vitest run`: same unrelated `VenuesPage` flake under parallel load (passes isolated 2/2).

## Phase 6: Manual dev-DB verification (pending — owner login)

- [ ] 6.1 `/campeones`: newest season on top and expanded, older seasons collapsed; expanding one shows Torneo → División → tarjetas.
