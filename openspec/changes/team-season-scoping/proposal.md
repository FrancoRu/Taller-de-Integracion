# Proposal: Season-Scope a Team's Tournament Participation (HU-98 / R2 / INC-2)

## Intent

A team's participation in a tournament is not season-scoped. `Team.TournamentId` is a single reassignable FK, and `RegisterTeamsToTournamentAsync` destructively reassigns/nulls it every call, so "Colón SF 2026" and "Colón SF 2027" collide and each "teams in tournament X" listing silently drops teams once reassigned. Players are already season-scoped via `PlayerTeamRegistration`; teams are the remaining gap. Goal: preserve per-season team↔tournament participation and history by mirroring that shipped pattern.

## Scope

### In Scope
- New `TeamTournamentRegistration` join entity (TeamId, TournamentId, unique index), repository + `IUnitOfWork` wiring, DbContext DbSet/config, seed builder.
- `RegisterTeamsToTournamentAsync` upserts registrations instead of destructive FK reassignment (mirror `EnsureRegistrationAsync`).
- `GetAllTeamsAsync` TournamentId filter switches from FK equality to a join predicate (mirror the `StageId` special-case).
- Keep `Team.TournamentId` as a denormalized "current-season" pointer.
- Idempotent two-step data migration (backfill).
- Backend tests; minimal frontend verification/regression.

### Out of Scope (Non-Goals)
- HU-99 stable `Club` identity (Could / Fase 2).
- HU-05 roles/permissions.
- Any playoff, stats, scorer, or sanction work (already season-correct by construction).
- Cross-season UI rollups beyond keeping current views working.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `team-tournament-registration`: registration semantics change from destructive FK reassign/null to join-table upsert that preserves prior-season participation; the "unassign"/"empty list clears members" behavior no longer erases history. Team-by-tournament listing resolves via the join, not FK equality.

## Approach

Approach 1 from exploration — direct mechanical analog of `PlayerTeamRegistration`. Join table becomes the source of truth; denormalized pointer keeps existing reads working; zero changes to Match/Scorer/PlayerStatistic/PlayerSanction/StageTeamMatch (all key off Stage→Division→Tournament).

**Data migration (idempotent, against unique (TeamId, TournamentId) index):**
1. `INSERT ... SELECT DISTINCT` from `StageTeamMatches → Stages → Divisions` (recovers real historical participation).
2. `INSERT ... SELECT` from `Teams.TournamentId IS NOT NULL` guarded by `NOT EXISTS`.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Domain/Entities/Models/Team.cs` | Modified | Keep `TournamentId`; add registrations nav |
| `Domain/Entities/Models/TeamTournamentRegistration.cs` | New | Join entity |
| `Application/Services/TeamService.cs` | Modified | Upsert register + join-predicate filter |
| Repository + `IUnitOfWork`/`UnitOfWork` | New/Modified | Mirror player registration wiring |
| `Infrastructure/Persistance/ApplicationDBContext.cs` + new EF migration | Modified/New | DbSet, config, backfill |
| `SampleTournamentBuilder.cs` | Modified | Seed new table |
| `Application/DTOs/Team/Request/GetTeamsFilteredRequest.cs` | Modified | Filter support |
| Frontend (EquiposStep, TeamRegisterPage, `team.d.ts`, submitWizard, e2e) | Verify/Minimal | Confirm set-replacement contract; regression only |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Exceeds 800-line review budget | High | Flag in tasks phase; slice or `size:exception` |
| `GetAllTeamsAsync` filter regresses `StageId` expression | Med | Mirror existing special-case exactly; test |
| Backfill under-migrates (inconsistent `Division.TournamentId`) | Med | Dry-run SELECT vs prod snapshot before apply |
| `Team.TournamentId` residual foot-gun | Low | Accepted (same as `Player.TeamId`) |
| HU-99 scope creep | Low | Explicit non-goal |

## Rollback Plan

Revert via `dotnet ef migrations remove` / down-migration dropping the `TeamTournamentRegistration` table (drops backfilled rows; no source data lost — history is recoverable from StageTeamMatches). Revert `TeamService`/DbContext/frontend commits. `Team.TournamentId` and all reads remain intact throughout, so the denormalized pointer keeps the app functional even mid-rollback.

## Dependencies

- Shipped `PlayerTeamRegistration` pattern (reference implementation).
- Supabase Postgres (`"Club12"` schema, `gen_random_uuid()` enabled).

## Success Criteria

- [ ] Registering the same team to two tournaments preserves both season participations.
- [ ] "Teams in tournament X" lists all season-registered teams via the join.
- [ ] Backfill is idempotent and recovers historical participation from StageTeamMatches.
- [ ] Match/stat/sanction/roster behavior unchanged; backend + frontend tests green.

## Proposal question round

Framing is orchestrator-supplied (adopt Approach 1). No user turn available to ask directly; these assumptions need review before spec/design if the user disagrees:
- Backfill trusts `Division.TournamentId` as consistent; inconsistent rows would under-migrate (dry-run planned).
- "Unassign / empty list clears members" no longer erases history — treated as a deliberate behavior change, not a regression.
- Frontend stays minimal: denormalized pointer keeps current views working; full cross-season history UI is deferred.
