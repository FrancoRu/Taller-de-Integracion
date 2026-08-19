# Admin Test Data Tools — Design

**Date:** 2026-08-18
**Status:** Approved for implementation

## Purpose

Give Admin users a self-service way to reset the Supabase dev database to a clean,
realistic tournament dataset — without a DBA, a SQL console, or redeploying the app.
The Supabase database currently backing `club12.argentum-solutions.com.ar` holds no
real data yet, so this is safe to build now; it must never be reachable once real
user data exists (see **Guardrails** below).

Two admin-only actions, reachable from a new "Test" tab in the panel sidebar:

- **Borrar DB** — wipes all tournament-domain data (tournaments, teams, players,
  matches, sanctions, statistics, blog posts, venues). Leaves Identity (users,
  roles) untouched — nobody loses their session or account.
- **Cargar Datos de prueba** — seeds 2 complete, realistic tournaments (divisions,
  teams, players, stages, matches with scores/scorers/statistics, sanctions).
  Refuses to run against a non-empty database instead of silently duplicating data.

## Non-goals

- Not a general-purpose backup/restore tool.
- Not exposed anywhere outside the Admin-gated panel route.
- Does not touch Identity/Users/Roles tables.
- Does not replace or change the existing startup `DataSeeder` (still seeds 1
  tournament on first boot of an empty dev database, unchanged behavior).

## Architecture

### Backend

**New: shared sample-data builder**

Extract the entity-construction logic currently private to `DataSeeder`
(`Club12-Backend/Infrastructure/Persistance/DataSeeder.cs`) — building a division
with teams and players, round-robin matches with scores/scorers/statistics,
sanctions, blog posts — into an internal, reusable builder
(`Infrastructure/Persistance/SampleTournamentBuilder.cs`) that:

- Builds **one** fully-populated `Tournament` (divisions, teams, players, stages,
  matches, sanctions) given a name, slug seed, and date range, so it can be called
  multiple times with different tournament metadata.
- Is used by both `DataSeeder.SeedAsync()` (startup path, still calls it once, same
  1-tournament behavior as today) and the new `DataMaintenanceService` (calls it
  twice, for 2 distinct tournaments — different names, dates, and team rosters so
  they're visibly distinct in the UI, not copies of each other).
- Blog posts stay owned by `DataSeeder` (not tournament-specific, not duplicated by
  `SampleTournamentBuilder`); `DataMaintenanceService.SeedSampleDataAsync()` adds
  its own 2 blog posts referencing both new tournaments.

**New: `IDataMaintenanceService` / `DataMaintenanceService`**
(`Application/Interfaces/Services/IDataMaintenanceService.cs`,
`Infrastructure/Persistance/DataMaintenanceService.cs` — same interface-in-Application,
implementation-in-Infrastructure split as `IDivisionService`/`DivisionService`)

```csharp
public interface IDataMaintenanceService
{
    Task<DataWipeResult> WipeSampleDataAsync(CancellationToken ct = default);
    Task<DataSeedResult> SeedSampleDataAsync(CancellationToken ct = default);
}
```

`WipeSampleDataAsync()`:

1. Opens a single DB transaction (`db.Database.BeginTransactionAsync()`).
2. Deletes rows via `ExecuteDeleteAsync()` (EF Core bulk delete — no entities
   materialized), **in this exact order** (children before parents — explicit
   order chosen so correctness does not depend on FK cascade configuration):

   ```
   Scorer
   PlayerStatistic
   PlayerSanction
   StageTeamMatch
   PlayerTeamRegistration
   Match
   Player
   Stage
   Team
   Division
   Tournament
   Venue
   BlogPost
   ```

3. Commits the transaction. On any exception, the transaction rolls back — the
   database is left exactly as it was before the call (never half-wiped).
4. Returns row counts per entity for the UI summary/toast.

`SeedSampleDataAsync()`:

1. Checks `await db.Tournaments.AnyAsync(ct)`. If true, throws
   `InvalidOperationException` with a message telling the caller to wipe first —
   the controller maps this to `409 Conflict`. No silent duplication, ever.
2. Otherwise calls `SampleTournamentBuilder` twice with two distinct tournament
   definitions (see **Sample dataset shape** below), adds both to the context,
   adds 2 blog posts, `SaveChangesAsync()`.
3. Returns counts (tournaments, divisions, teams, players, matches, sanctions) for
   the UI summary/toast.

**New: `DataMaintenanceController`**
(`API/Controllers/DataMaintenanceController.cs`)

```csharp
[ApiController]
[Route("api/data-maintenance")]
[Authorize(Roles = Roles.Admin)]
public class DataMaintenanceController(IDataMaintenanceService service) : ControllerBase
{
    [HttpPost("wipe")]
    public async Task<ActionResult<DataWipeResult>> Wipe(CancellationToken ct);

    [HttpPost("seed")]
    public async Task<ActionResult<DataSeedResult>> Seed(CancellationToken ct);
}
```

`Roles.Admin` already exists (`Domain/Enums/Roles.cs:13`) — exact match for
"only when role is Admin", no new role/policy needed.

### Sample dataset shape (2 tournaments)

Both tournaments follow the same internal shape as the current `DataSeeder`
(2 divisions each — Primera División + Reserva —, 4 teams per division, 8 players
per team, a Group-stage round robin with roughly half the matches finished with
scores/scorers/statistics and half upcoming, 1-2 sanctions per division), but with
distinct identities so they read as two real, separate tournaments rather than
clones:

| | Tournament 1 | Tournament 2 |
|---|---|---|
| Name | Torneo Apertura 2026 | Torneo Clausura 2026 |
| Status | `Ongoing` | `Ongoing` |
| Team rosters | current `DataSeeder` names (Atlético Central, Deportivo Norte, ...) | a second distinct roster (new names, codes, colors — not overlapping the first) |
| Dates | Jun–Sep 2026 | Oct 2026–Jan 2027 |

Total: 2 tournaments, 4 divisions, 16 teams, 128 players, 2 stages' worth of
round-robin matches, several sanctions, 2 blog posts — big enough to look real
when browsing the deployed app, small enough to seed in well under a second.

### Frontend

- `Club12-WebClient/src/modules/core/constants/appRoutes.ts`: add
  `panelTest: '/panel/test'`.
- `Club12-WebClient/src/views/core/components/SidebarLayout.tsx`: one more entry
  in `TABS_BY_ROLE[UserRolesType.Admin]`, placed right after the `Estadisticas`
  entry:
  ```ts
  { label: 'Test', path: APP_ROUTES.panelTest, icon: TAB_ICONS['Test'] }
  ```
  (Admin-only automatically — this array is keyed by role, nothing extra needed.)
- `Club12-WebClient/src/App.tsx`: register the `panelTest` route following the
  existing pattern used by the other `/panel/*` routes (same role-guard wrapper).
- New view `Club12-WebClient/src/views/test/testDataPage.tsx`: two buttons.
  - **Borrar DB**: `confirmDelete({ title: '¿Borrar todos los datos de prueba?', text: '...' })`
    from `@/modules/core/utils/confirmDialog` (existing SweetAlert2 helper) before
    calling the wipe endpoint. `notifySuccess`/`notifyError` after.
  - **Cargar Datos de prueba**: no confirmation (non-destructive) — calls the seed
    endpoint directly, `notifySuccess`/`notifyError` after. On a `409` from the
    backend (DB not empty), shows the error message telling the admin to wipe
    first.
- New service `Club12-WebClient/src/modules/dataMaintenance/service/dataMaintenance.service.ts`
  with `wipeSampleData()` / `seedSampleData()`, following the same HTTP client
  pattern as the existing `*.service.ts` files.

## Guardrails

- Both endpoints are `[Authorize(Roles = Roles.Admin)]` — same mechanism already
  protecting every other admin-only endpoint in this codebase.
- Wipe never touches Identity — confirmed design decision, nobody can lock
  themselves (or anyone else) out by clicking the button.
- Wipe runs inside one transaction — no partially-wiped state on failure.
- Seed refuses to run on a non-empty database — no silent duplication.
- **Not addressed by this change, flagged for follow-up:** nothing currently stops
  this feature from being used once the Supabase database holds real user/tournament
  data. Before that happens, this needs an extra guard (e.g. an environment/config
  flag, `Seed:AllowDataMaintenance`, checked by the controller/service, off by
  default in any environment with real data) so the buttons become inert rather
  than deleting real tournaments. Out of scope for this change per explicit user
  instruction ("por el momento la DB de supabase no tendrá datos reales"), but
  tracked here so it isn't forgotten.

## Testing (TDD — written before the implementation)

New file `Club12-Backend/API.Tests/DataMaintenanceTests.cs`, integration tests via
the existing `CustomWebApplicationFactory` (SQLite in-memory), mirroring the style
of `AuthorizationGatingTests.cs`:

1. `WipeSampleDataAsync_RemovesTournamentDomainData_KeepsIdentityIntact` — seed
   data (reuse `SampleTournamentBuilder` or the existing per-test seed helpers),
   call wipe, assert all tournament-domain tables are empty and
   `IdentityAppDbContext` users/roles are unchanged.
2. `SeedSampleDataAsync_OnEmptyDatabase_Creates2TournamentsWithExpectedShape` —
   assert 2 tournaments, 4 divisions, 16 teams, 128 players, matches with
   scores/scorers/statistics on the finished half, sanctions present.
3. `SeedSampleDataAsync_OnNonEmptyDatabase_ReturnsConflict` — seed once, call seed
   again, assert `409` and that no additional rows were created.
4. `Wipe_And_Seed_Endpoints_RejectNonAdminRoles` — same pattern as
   `AuthorizationGatingTests.GetPlayerCompleteData_WrongRole_ReturnsForbidden`,
   parameterized over the non-Admin roles.

## Files touched

**New:**
- `Club12-Backend/Infrastructure/Persistance/SampleTournamentBuilder.cs`
- `Club12-Backend/Application/Interfaces/Services/IDataMaintenanceService.cs`
- `Club12-Backend/Infrastructure/Persistance/DataMaintenanceService.cs`
- `Club12-Backend/API/Controllers/DataMaintenanceController.cs`
- `Club12-Backend/API.Tests/DataMaintenanceTests.cs`
- `Club12-WebClient/src/views/test/testDataPage.tsx`
- `Club12-WebClient/src/modules/dataMaintenance/service/dataMaintenance.service.ts`

**Modified:**
- `Club12-Backend/Infrastructure/Persistance/DataSeeder.cs` (delegates entity
  construction to `SampleTournamentBuilder`, behavior unchanged)
- `Club12-Backend/API/Utils/StartupExtensions.cs` (register
  `IDataMaintenanceService` in DI, same section as the other service registrations)
- `Club12-WebClient/src/modules/core/constants/appRoutes.ts`
- `Club12-WebClient/src/views/core/components/SidebarLayout.tsx`
- `Club12-WebClient/src/App.tsx`
