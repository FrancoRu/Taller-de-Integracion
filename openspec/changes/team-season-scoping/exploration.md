# Exploration — team-season-scoping (HU-98 / R2 / INC-2)

> Phase: sdd-explore · Store: hybrid (Engram topic `sdd/team-season-scoping/explore` + this file)
> Read-only investigation. No production code changed.

## Current State

**Players are already season-scoped — this is the pattern to mirror.**
`PlayerTeamRegistration` (`Club12-Backend/Domain/Entities/Models/PlayerTeamRegistration.cs`) is a join
entity (PlayerId, TeamId, TournamentId, unique index on PlayerId+TournamentId). `Player.TeamId` stays
as a denormalized "current team" pointer; the registration table is the source of truth.
`PlayerService.EnsureRegistrationAsync` (`Application/Services/PlayerService.cs:70`) upserts one
registration per (player, tournament). `TeamService.AttachSeasonRostersAsync`
(`Application/Services/TeamService.cs:182`) scopes `Team.Players` by season via that table. Shipped
via migration `20260817082125_AddPlayerTeamRegistrationTable.cs` with a backfill.

**Teams are NOT season-scoped — the remaining gap.** `Team.TournamentId` (`Domain/Entities/Models/Team.cs:21`)
is a single nullable, reassignable FK. `TeamService.RegisterTeamsToTournamentAsync` (`TeamService.cs:220`)
destructively reassigns/nulls that FK on every call — no team-side registration table. Every "teams in
tournament X" listing (admin TeamsPage, public tournament page, wizard EquiposStep, TeamRegisterPage)
silently loses a team once it's reassigned.

**Scoping fact confirmed by code**: `Match`, `Scorer`, `PlayerStatistic`, `PlayerSanction` and
`StageTeamMatch` all key off `Stage → Division → Tournament` (fixed at creation) plus `Team.Id`/`Player.Id`
directly — none read `Team.TournamentId`. Historical match/stat/sanction data is NOT corrupted by
reassignment today. The bug is narrower: team↔tournament *listing/association* queries.

## Affected Areas

Backend:
- `Domain/Entities/Models/Team.cs` — keep `TournamentId` as denormalized current-season pointer; add registration navigation.
- NEW `TeamTournamentRegistration.cs` mirroring `PlayerTeamRegistration.cs`.
- `Application/Services/TeamService.cs` — `RegisterTeamsToTournamentAsync` upserts registrations (mirror `EnsureRegistrationAsync`); `GetAllTeamsAsync` TournamentId filter switches from FK equality to a join predicate (same special-cased pattern already used for `StageId` at `TeamService.cs:139-145`).
- New repository interface/impl + `IUnitOfWork`/`UnitOfWork` wiring, mirroring `PlayerTeamRegistrationRepository`.
- `Application/DTOs/Team/Request/GetTeamsFilteredRequest.cs`, `Infrastructure/Persistance/ApplicationDBContext.cs` (new DbSet + config), `Infrastructure/Persistance/SampleTournamentBuilder.cs` (seed new table).
- New EF migration mirroring the player one. New `TeamTournamentRegistrationTests.cs`.

Frontend (narrow):
- `src/views/tournament/wizard/steps/EquiposStep.tsx`, `src/views/team/TeamRegisterPage.tsx`, `src/modules/team/type/team.d.ts` (`ITeamResponse.tournamentId`) — only if UX must show full season history; otherwise denormalized pointer keeps them working.
- `src/views/tournament/wizard/submitWizard.ts` — depends on `RegisterTeamsToTournamentAsync` set-replacement contract (comment lines 218-222); re-verify.
- e2e `01-wizard-clausura.spec.ts` / `02-wizard-femenino-clausura.spec.ts` — regression risk if EquiposStep grouping changes.
- Other `tournamentId` occurrences (matchesPage, stagesPage, playerSanctionCreatePage, divisionPage, TournamentEditPage) are Division/Stage/Match/route-param ids, NOT `Team.tournamentId`.

## Approaches

1. **`TeamTournamentRegistration` join entity mirroring `PlayerTeamRegistration`** — Team stays one reusable row; `TournamentId` stays denormalized "current" pointer; registration table is source of truth.
   - Pros: reuses an already-tested pattern; zero changes to Match/Scorer/Stat/Sanction/StageTeamMatch; minimal frontend impact; matches HU-98's explicit instruction; stays in Must scope. Effort: Medium.
   - Cons: `Team.TournamentId` remains a residual foot-gun (same accepted risk as `Player.TeamId`); every "list teams by tournament" call site must be audited.
2. **HU-99 "stable club identity"** — new `Club` parent; `Team` becomes disposable per-season row.
   - Pros: cleanest long-term for cross-season rollups. Cons: HU-99 is Could/Fase 2 → scope creep; migrates Name/Slug/ThreeLetterCode/LogoUrl/ShirtColor to `Club`, touches every `TeamId` FK. Effort: High.

## Recommendation

**Approach 1.** Direct mechanical analog HU-98/R2 asks for; reuses a reviewed/tested pattern; defers HU-99 (Could).

**Data migration plan**: teams can recover real history (unlike players). Backfill in two idempotent steps against the unique (TeamId, TournamentId) index:
1. `INSERT ... SELECT DISTINCT` from `StageTeamMatches → Stages → Divisions` for real historical participation.
2. `INSERT ... SELECT` from `Teams.TournamentId IS NOT NULL` guarded by `NOT EXISTS` for teams registered but not yet assigned to a stage.

## Risks

- **Review-budget size**: likely exceeds a single small PR once entity + repo + UnitOfWork + migration + service/filter rewrite + seed + tests (+ any frontend) are counted. Session budget is 800 lines / single-pr; if the tasks forecast exceeds it, stop and record `size:exception` or slice.
- **EF migration on Supabase Postgres**: schema-qualified (`"Club12"`), `gen_random_uuid()` already enabled; dry-run the backfill SELECT against a prod snapshot (inconsistent `Division.TournamentId` could under-migrate).
- **Coupling with stats/sanctions/rosters is LOW** (confirmed). Real risk: `GetAllTeamsAsync` filter rewrite (must not regress the `StageId` special-cased expression) and `submitWizard.ts` set-replacement dependence.
- **HU-99 scope creep** — explicitly out of scope.
- Test harness `CustomWebApplicationFactory` builds schema via `EnsureCreated()` from the live EF model — new table needs no special handling.

## Already Done (do not redo)

- The entire player half of HU-98 is shipped (reference implementation).
- `TeamService.GetTeamByIdAsync`/`GetTeamByIdOrSlugAsync` already accept an optional `tournamentId` and return season-correct rosters — "view a team in a past season" already works; only "list/associate teams by tournament" is the gap.
- `StageTeamMatch` and all match/stat/sanction entities are already season-correct by construction.

## Ready for Proposal: Yes
