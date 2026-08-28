# Design: Season-Scope a Team's Tournament Participation (HU-98)

## Technical Approach

Mechanical analog of the shipped `PlayerTeamRegistration`. A new `TeamTournamentRegistration`
join entity becomes the source of truth for team↔tournament participation; `Team.TournamentId`
stays as a denormalized "current-season" pointer (exactly like `Player.TeamId`).
`RegisterTeamsToTournamentAsync` upserts registrations scoped to the target tournament;
`GetAllTeamsAsync`'s tournament filter resolves via a join predicate mirroring the existing
`StageId` special-case. Clean Architecture layering is preserved: Domain (entity + nav),
Application (interface, service, DTO), Infrastructure (repo, DbContext config, migration, seed).

## Architecture Decisions

| # | Decision | Alternatives rejected | Rationale |
|---|----------|-----------------------|-----------|
| 1 | Keep `Team.TournamentId` as denormalized current pointer; add `TeamTournamentRegistration` as source of truth | Drop the FK; HU-99 stable `Club` identity | Zero read regressions for the many "current season" call sites; identical to the accepted `Player.TeamId` tradeoff; HU-99 is out of scope (Could/Fase 2) |
| 2 | Upsert scoped to the **target tournament only** (empty list clears just that tournament's rows) | Global set-replacement (current destructive behavior) | Preserves other-season registrations; the within-tournament set-replacement contract still holds, so `submitWizard.ts` is unaffected |
| 3 | Tournament filter = registration join predicate, suppress the auto FK-equality via a new `ignoredProperties` param on `ConstructFilterExpression` | Add `TournamentId` to global `ShouldSkipProperty`; AND the join onto the auto equality | Global skip breaks Division/Match tournament filters (their FK is authoritative); AND-ing over-restricts (a reassigned team registered to X but pointing at Y would be dropped). Additive param is backward-compatible and touches only TeamService |
| 4 | Two-step idempotent backfill against the unique index | Single `Teams.TournamentId` backfill | Teams (unlike players) have recoverable real history via `StageTeamMatch → Stage → Division`; step 1 recovers it, step 2 covers registered-but-unstaged teams |

## Data Flow — Register Teams (Upsert)

```
Controller ──RegisterTeamsToTournamentAsync(tournament, teamIds)──▶ TeamService
   1. existing = registrationRepo.Find(r => r.TournamentId == tournament.Id)
   2. remove existing rows whose TeamId ∉ teamIds        (scoped to this tournament)
   3. add rows for teamIds not already registered         (new TeamTournamentRegistration)
   4. affected = teamRepo.Find(id ∈ teamIds OR TournamentId == tournament.Id)
      · id ∈ teamIds        → TournamentId = tournament.Id   (refresh pointer)
      · else (== tournament)→ TournamentId = null            (clear pointer)
   5. teamRepo.UpdateRange(affected) ──▶ UnitOfWork.SaveChanges
```

Read path: `GetAllTeamsAsync(filter.TournamentId)` → `team.TeamTournamentRegistrations.Any(r => r.TournamentId == filter.TournamentId)` → `AttachSeasonRostersAsync(teams, filter.TournamentId)` (unchanged).

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Domain/Entities/Models/TeamTournamentRegistration.cs` | Create | Join entity: `TeamId`, `TournamentId`, navs; XML docs mirroring `PlayerTeamRegistration` |
| `Domain/Entities/Models/Team.cs` | Modify | Add `ICollection<TeamTournamentRegistration> TeamTournamentRegistrations`; keep `TournamentId` |
| `Application/Interfaces/Repositories/ITeamTournamentRegistrationRepository.cs` | Create | `: IGenericRepository<TeamTournamentRegistration>` |
| `Application/Interfaces/Repositories/IUnitOfWork.cs` | Modify | Add `TeamTournamentRegistrationRepository` property |
| `Application/Utils/Extensions/QueryableExtensions.cs` | Modify | Add `params string[] ignoredProperties` to `ConstructFilterExpression` (default empty) |
| `Application/Services/TeamService.cs` | Modify | Rewrite `RegisterTeamsToTournamentAsync` (upsert); rewrite `TournamentId` branch in `GetAllTeamsAsync` |
| `Infrastructure/Repositories/TeamTournamentRegistrationRepository.cs` | Create | `: GenericRepository<...>` (auto-registered by convention scan in `StartupExtensions`) |
| `Infrastructure/Repositories/UnitOfWork.cs` | Modify | Add ctor param + property |
| `Infrastructure/Persistance/ApplicationDBContext.cs` | Modify | Add `DbSet<TeamTournamentRegistration>` |
| `Infrastructure/Persistance/Configurations/TeamTournamentRegistrationEntityConfiguration.cs` | Create | Unique index `(TeamId, TournamentId)`, FKs, `ToTable(EntityConstants.Tables.TeamTournamentRegistration, Schema)` |
| `Infrastructure/Persistance/EntityConstants.cs` | Modify | Add `TeamTournamentRegistration = "TeamTournamentRegistrations"` |
| `Infrastructure/Migrations/<ts>_AddTeamTournamentRegistrationTable.cs` | Create | Table + indexes + two-step backfill |
| `Infrastructure/Persistance/SampleTournamentBuilder.cs` | Modify | Seed a `TeamTournamentRegistration` per team (mirror player seed) |
| `API.Tests/TeamTournamentRegistrationTests.cs` | Create | Mirror player tests + upsert/coexistence/listing behavior |

DI note: repository auto-registration is convention-based (`*Repository` suffix scan in `StartupExtensions`), so no manual `AddScoped` line — only `UnitOfWork`/`IUnitOfWork` wiring is manual.

## Interfaces / Contracts

```csharp
public class TeamTournamentRegistration : EntityBase
{
    public required Guid TeamId { get; set; }
    public Team? Team { get; set; }
    public required Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
}
// Config: builder.HasIndex(r => new { r.TeamId, r.TournamentId }).IsUnique();

// GetAllTeamsAsync — mirror StageId, after ignoring the auto FK equality:
expression = QueryableExtensions.ConstructFilterExpression<Team, GetTeamsFilteredRequest>(
    filter, nameof(GetTeamsFilteredRequest.TournamentId));
if (filter.TournamentId.HasValue)
    expression = expression.And(t => t.TeamTournamentRegistrations
        .Any(r => r.TournamentId == filter.TournamentId.Value));
```

## Testing Strategy (Strict TDD — RED first)

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Integration (backend) | Register same team to two tournaments → both rows survive | `TeamTournamentRegistrationTests` via DI scope + SQLite factory |
| Integration | Empty list clears only the target tournament's rows | Assert other-tournament rows intact |
| Integration | Re-register (upsert) does not duplicate; unique index holds | `Assert.Single` |
| Integration | `GetAllTeamsAsync(TournamentId)` lists via join, incl. a team whose pointer moved away | Assert reassigned-but-registered team appears |
| Integration | `StageId` filter still works alongside `TournamentId` | Combined-filter test guards the special-case |
| Integration | Backfill join logic (StageTeamMatch→Stage→Division) as LINQ | Mirror player `Backfill_JoinLogic` test |
| Frontend | None required — denormalized pointer keeps views working; within-tournament set-replacement contract preserved | Re-run existing Vitest + e2e wizard specs as regression only |

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary. Pure DB/service change behind existing controllers.

## Migration / Rollout

`dotnet ef migrations add AddTeamTournamentRegistrationTable`. Table in schema `"Club12"`,
`gen_random_uuid()` PK, unique index `(TeamId, TournamentId)`, FKs (cascade) to Teams/Tournaments.
Backfill (idempotent, both guarded against the unique index):

1. `INSERT ... SELECT DISTINCT stm."TeamId", d."TournamentId"` from `StageTeamMatches → Stages → Divisions` (recovers real historical participation).
2. `INSERT ... SELECT t."Id", t."TournamentId"` from `Teams WHERE "TournamentId" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TeamTournamentRegistrations r WHERE r."TeamId"=t."Id" AND r."TournamentId"=t."TournamentId")`.

Dry-run both SELECTs against a prod snapshot before apply (inconsistent `Division.TournamentId` could under-migrate).

**Rollback**: `Down()` drops the table (drops backfilled rows — history remains recoverable from
StageTeamMatches). Revert TeamService/DbContext/frontend commits. `Team.TournamentId` and all reads
stay intact throughout, so the app stays functional mid-rollback.

## Review Budget

Entity + repo + UoW + DbContext/config + migration + service rewrite + seed + tests likely exceeds
the 800-line single-PR budget. `sdd-tasks` MUST forecast and either slice (backend-core → migration/backfill → tests)
or record `size:exception`.

## Open Questions

- [ ] Confirm `Division.TournamentId` is consistent enough for backfill step 1 (dry-run planned).
- [ ] Confirm empty-list "no longer erases history" is an accepted deliberate behavior change (not a regression).
