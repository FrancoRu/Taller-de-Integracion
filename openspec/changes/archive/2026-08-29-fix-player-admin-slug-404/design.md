# Design: Admin Player Detail Accepts Slug + Unified Player Slug Format

## Technical Approach

**Part 1** widens one API-layer route. `PlayerController.GetPlayerByIdCompleteDataAsync`
drops its `:guid` constraint and delegates to the already-shipped
`PlayerService.GetPlayerByIdOrSlugAsync`. Application, Domain and Infrastructure
are untouched: the admin action only ever differed from the public one in its
*authorization* and its *response DTO*, never in its data access.

**Part 2** collapses the two competing slug-source strings into a single Domain
rule (`Player.BuildSlugSource`), consumed by Application (`CreatePlayerAsync`),
Infrastructure (`SampleTournamentBuilder`) and mirrored by SQL in the backfill —
plus a `SlugRegistry.ForPlayer` bucket and a reversible re-backfill migration.
Layering holds: the concat rule is a naming invariant of the `Player` entity, so
it lives in Domain and is called *downward* by both outer layers.

## Architecture Decisions

| # | Decision | Alternatives rejected | Rationale |
|---|----------|-----------------------|-----------|
| 1 | Reuse `GetPlayerByIdOrSlugAsync` as-is; **no** admin/"complete data" overload | Add `GetPlayerByIdOrSlugAsync(bool includeCompleteData)`; add `includes` to the admin path | Verified: `PlayerRepository` is an empty `GenericRepository<Player>`; `GetPlayerByIdAsync` calls `GetByIdAsync(id, includes: null)` and the slug branch calls `FindAsync(pred, includes: null)`. **Both load zero navigations.** "Complete data" is a DTO concern only — `AdminPlayerResponse` adds `DocumentNumber`, `BirthDate`, `PhoneNumber`, `SocialSecurity`, `IsFederated`, `Club`, `Category`, all scalar columns on `Player`. An overload would add API surface for zero behavior change |
| 2 | Slug source helper lives in **Domain** (`Player.BuildSlugSource`), with `FullName` delegating to it | `Application/Utils/Helper/Slug/PlayerSlugSource.cs` | The concat rule is the same rule `FullName` already encodes; splitting them across layers recreates the divergence we are deleting. Domain has no outward dependency, so Application and Infrastructure can both call it |
| 3 | Reversible backfill via a **snapshot side table**, restored in `Down()` | Deterministic recompute in `Down()` | Recompute is **impossible**. Pre-change slugs are a *mixture*: seed rows are `apellido-nombre-dni` (recomputable from `DocumentNumber`), create/backfill rows are `apellido-nombre[-N]` where `N` came from insertion order. Nothing in the row distinguishes the two provenances, and once the new backfill renumbers collisions the old `N` is unrecoverable. Snapshot is the only correct inverse |
| 4 | Collision ordering `ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY "Id")` | `ORDER BY "DateCreated"` | Exact precedent from migration `20260828003816` (all four tables). `Id` is a unique, immutable `uuid` → total order, no ties, byte-identical result on rerun. `DateCreated` defaults to `DateTime.UtcNow` at *construction* time, so a bulk seed batch clusters and can tie → non-deterministic suffixes |
| 5 | Two-phase UPDATE (park on `'__tmp_' \|\| "Id"`, then assign final) | Single UPDATE; `DEFERRABLE` constraint | `IX_Players_Slug` is a plain unique **index**, which PostgreSQL cannot defer. A single UPDATE that permutes slugs (row A takes row B's old value) trips the index mid-statement. The `__tmp_` prefix contains `_`, a character `SlugGenerator`'s `[^a-z0-9]+` rule can never emit, so the parking values are disjoint from every real and every final slug |
| 6 | `SlugRegistry` gains a third `HashSet` + `ForPlayer`, no new plumbing | Thread a separate player registry through the builder | `BuildDivision` already receives `slugRegistry` (`SampleTournamentBuilder.cs:271`) and the player loop is inside it (`:314-337`). The bucket is per-table because each table has its own unique index — same reason `_divisionSlugs`/`_stageSlugs` are separate |
| 7 | Ship as **chained PRs**: Part 1 → Part 2 | Single PR | See Review Budget |

## Route Disambiguation — precise claim

The exploration says "the literal `admin` segment wins". That is true but weaker
than reality: among `PlayerController`'s GET routes only
`api/players/admin/{idOrSlug}` has **four** segments (`{idOrSlug}`, `public` and
`""` are three or fewer). `/api/players/admin/lopez-carlos` therefore has exactly
one candidate — there is no precedence contest at all.

Literal-over-parameter precedence is still the *supporting* guarantee, and it is
already proven in production by `[HttpGet("public")]` (`:218`) coexisting with
`[HttpGet("{idOrSlug}")]` (`:108`).

Pre-existing, unchanged edge case: a player whose slug is literally `admin`
resolves via the **public** three-segment route, not the admin one.

Locked by test: `GET api/players/admin/{slug}` → 200 (never routed to the public
action, asserted via the `AdminPlayerResponse`-only field `documentNumber` being
present in the body).

## Sequence — Admin "Ver" (slug and GUID inputs)

```
PlayersPage        Router          player.service      PlayerController      PlayerService       Repo/DB
    │                 │                   │                    │                   │               │
 handleView(row)      │                   │                    │                   │               │
    ├─ navigate(/panel/jugadores/lopez-carlos) ──▶             │                   │               │
    │                 ├─ PlayerPage: playerId = "lopez-carlos"  │                  │               │
    │                 ├─ getPlayerById(param, isAdministrative=true) ──▶           │               │
    │                 │                   ├─ GET /api/players/admin/lopez-carlos ─▶│               │
    │                 │                   │        [Authorize(AdminOrOwner)]       │               │
    │                 │                   │        route: admin/{idOrSlug}  (4 segments, 1 match)  │
    │                 │                   │                    ├─ GetPlayerByIdOrSlugAsync("lopez-carlos")
    │                 │                   │                    │   Guid.TryParse ⇒ FALSE           │
    │                 │                   │                    │                   ├─ FindAsync(p => p.Slug == "lopez-carlos")
    │                 │                   │                    │                   │   SELECT … WHERE "Slug" = $1  (exact/ordinal)
    │                 │                   │                    │◀── Player | null ─┤◀──────────────┤
    │                 │                   │◀─ 200 AdminPlayerResponse ─┤ (null ⇒ 404 ProblemDetails)
    │                 │◀── render ────────┤                    │                   │               │

GUID input (matchPage.tsx:513 → navigate(build(sanction.playerId))) — same chain, one branch differs:
                                                     GetPlayerByIdOrSlugAsync("6f3e…")
                                                       Guid.TryParse ⇒ TRUE
                                                       └─ GetPlayerByIdAsync(guid) ─▶ GetByIdAsync(id, includes: null)
```

Both branches return an entity with **no navigations loaded** — identical to
today's `GetPlayerByIdAsync(Guid)` path, so the `AdminPlayerResponse` AutoMapper
projection is bit-for-bit unchanged.

## Interfaces / Contracts

```csharp
// API/Controllers/PlayerController.cs:131-146  (Part 1 — the whole change)
[Authorize(Roles = Roles.AdminOrOwner)]
[HttpGet("admin/{idOrSlug}")]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AdminPlayerResponse))]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<AdminPlayerResponse>> GetPlayerByIdCompleteDataAsync(string idOrSlug)
{
    Player? player = await playerService.GetPlayerByIdOrSlugAsync(idOrSlug);
    if (player is null) return this.NotFoundProblem(nameof(Player), idOrSlug);
    return Ok(mapper.Map<AdminPlayerResponse>(player));
}
// NotFoundProblem(this ControllerBase, string entity, object id) already takes object — no change.

// Domain/Entities/Models/Player.cs  (Part 2 — single slug-source truth)
public static string BuildSlugSource(string lastName, string firstName, string? secondName) =>
    string.IsNullOrWhiteSpace(secondName) ? $"{lastName} {firstName}"
                                          : $"{lastName} {firstName} {secondName}";

public string SlugSource => BuildSlugSource(LastName, FirstName, SecondName);
public string FullName   => BuildSlugSource(LastName.ToUpper(), FirstName, SecondName); // display unchanged

// Application/Services/PlayerService.cs:37-39 — FullName → SlugSource (behaviour-identical: slug lowercases)
playerEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
    playerEntity.SlugSource, c => _playerRepository.ExistsAsync(p => p.Slug == c));

// Infrastructure/Persistance/SampleTournamentBuilder.cs — SlugRegistry (:130-153)
private readonly HashSet<string> _playerSlugs = [];
public string ForPlayer(string source) => Register(source, _playerSlugs);

// …and at :327, DNI dropped:
Slug = slugRegistry.ForPlayer(Player.BuildSlugSource(lastName, firstName, secondName: null)),
```

`SlugRegistry` lifecycle is unchanged: one instance per persisted batch, created
in `DataSeeder.cs:272` and `DataMaintenanceService.cs:198`, threaded through every
`Build` call and down into `BuildDivision`. Adding `_playerSlugs` makes player
slugs unique across the whole 4-tournament / 8-players-per-team batch, which is
exactly what the DNI suffix was silently doing.

## Migration — reversible re-backfill

`dotnet ef migrations add RebackfillPlayerSlugsWithoutDocumentNumber --context ApplicationDBContext`
(produces `.cs` + `.Designer.cs`; the model snapshot is unchanged because this is
data-only, but the Designer file must be committed so
`EveryApplicationMigration_IsRegistered_WithMigrationAttribute` stays green).

**Up** — three statements:

```sql
-- 1. Rollback ledger (outside the EF model, so the differ ignores it).
DROP TABLE IF EXISTS "Club12"."PlayerSlugBackup_20260829";
CREATE TABLE "Club12"."PlayerSlugBackup_20260829" (
    "Id" uuid PRIMARY KEY, "OldSlug" character varying(220) NOT NULL);
INSERT INTO "Club12"."PlayerSlugBackup_20260829" ("Id","OldSlug")
SELECT "Id","Slug" FROM "Club12"."Players";

-- 2. Park every slug on a value the generator can never produce ('_' ∉ [a-z0-9-]).
UPDATE "Club12"."Players" SET "Slug" = '__tmp_' || "Id"::text;

-- 3. Assign canonical slugs; targets are distinct by construction and disjoint from '__tmp_%'.
WITH base AS (
    SELECT p."Id",
        trim(both '-' from regexp_replace(
            translate(lower(concat(p."LastName", ' ', p."FirstName",
                CASE WHEN p."SecondName" IS NULL OR trim(p."SecondName") = ''
                     THEN '' ELSE ' ' || p."SecondName" END)),
                'áéíóúüñ', 'aeiouun'),
            '[^a-z0-9]+', '-', 'g')) AS slug_base
    FROM "Club12"."Players" p),
numbered AS (
    SELECT "Id", slug_base,
           ROW_NUMBER() OVER (PARTITION BY slug_base ORDER BY "Id") AS rn
    FROM base)
UPDATE "Club12"."Players" t
SET "Slug" = CASE WHEN n.rn = 1 THEN n.slug_base ELSE n.slug_base || '-' || n.rn::text END
FROM numbered n WHERE t."Id" = n."Id";
```

Statement 3 is a verbatim reuse of the shipped `20260828003816` player backfill
(lines 133-163) — same concat, same `translate`/`regexp_replace`, same `ORDER BY "Id"`
suffix rule. That is deliberate: create, seed and backfill now share one rule.

**Down** — the true inverse, guarded so it never half-restores:

```sql
UPDATE "Club12"."Players" SET "Slug" = '__tmp_' || "Id"::text
WHERE to_regclass('"Club12"."PlayerSlugBackup_20260829"') IS NOT NULL;

UPDATE "Club12"."Players" t SET "Slug" = b."OldSlug"
FROM "Club12"."PlayerSlugBackup_20260829" b WHERE t."Id" = b."Id";

DROP TABLE IF EXISTS "Club12"."PlayerSlugBackup_20260829";
```

`Down` re-parks first for the same permutation reason as `Up`. Players inserted
*after* `Up` have no backup row and keep their canonical slug — correct, since no
prior value exists to restore. `Up` is idempotent under a Down→Up cycle because
it drops and recreates the ledger. `DROP TABLE IF EXISTS` in `Down` means a
double-`Down` is a safe no-op.

The ledger table persists after `Up`. That is intentional (it is the only inverse);
it may be dropped manually once the release is confirmed.

## File Changes

| File | Action | Part | Description |
|------|--------|------|-------------|
| `API/Controllers/PlayerController.cs:131-146` | Modify | 1 | Route `admin/{idOrSlug}`, param `string`, call `GetPlayerByIdOrSlugAsync`, XML doc updated |
| `API.Tests/PlayerSlugTests.cs` | Modify | 1 | Admin by slug / by GUID / unknown → 404 / route-not-shadowed |
| `API.Tests/AuthorizationGatingTests.cs` | Modify | 1 | Add a slug-shaped 401/403 case alongside the GUID ones |
| `Club12-WebClient/src/views/player/PlayersPage.test.tsx` | Create | 1 | "Ver" navigates to `/panel/jugadores/{row.slug}` |
| `Club12-WebClient/src/views/player/PlayerPage.test.tsx` | Create | 1 | Issues `GET /api/players/admin/{param}` (axios mocked) |
| `Domain/Entities/Models/Player.cs` | Modify | 2 | `BuildSlugSource` + `SlugSource`; `FullName` delegates |
| `Application/Services/PlayerService.cs:37-39` | Modify | 2 | `FullName` → `SlugSource` |
| `Infrastructure/Persistance/SampleTournamentBuilder.cs:130-153,327` | Modify | 2 | `ForPlayer` bucket; seed drops DNI |
| `Infrastructure/Migrations/<ts>_RebackfillPlayerSlugsWithoutDocumentNumber.cs` (+`.Designer.cs`) | Create | 2 | Ledger + two-phase re-backfill, reversible |
| `API.Tests/SampleTournamentBuilderSlugTests.cs` | Modify | 2 | Seeded player slugs are clean kebab, DNI-free, unique across the batch |

No frontend **source** change in either part.

## Testing Strategy (Strict TDD — RED first)

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Integration (P1) | `GET api/players/admin/{slug}` → 200, body `id` matches | `PlayerSlugTests` + `CustomWebApplicationFactory`, mirroring `GetPlayerById_BySlug_Returns200WithMatchingPlayer` |
| Integration (P1) | `GET api/players/admin/{guid}` → 200 (regression for `matchPage.tsx:513`) | Same fixture |
| Integration (P1) | Unknown id-or-slug → 404 `ProblemDetails` | Assert status + `application/problem+json` |
| Integration (P1) | Admin route is not shadowed by `{idOrSlug}` | Assert `documentNumber` present — an `AdminPlayerResponse`-only field |
| Integration (P1) | Anonymous → 401, Guest → 403 on the **slug** form | `AuthorizationGatingTests` |
| Unit (P2) | `Player.BuildSlugSource` — with/without `SecondName`; `FullName` still uppercases `LastName` | Pure xUnit, no fixture |
| Unit (P2) | Seeded player slugs: kebab-only, no 8-digit DNI run, no GUID, distinct across the batch | Extend `SampleTournamentBuilderSlugTests`, shared-registry variant |
| Integration (P2) | `CreatePlayerAsync` still emits `-2` on duplicate names | Existing `CreatePlayerAsync_DuplicateFullName_AppendsSuffixToSlug` as regression |
| Migration (P2) | **Not automatable here** — see gap below | Manual dev-DB procedure |
| Frontend (P1) | `PlayersPage` "Ver" URL; `PlayerPage` request URL | Vitest + Testing Library, characterization only |

### Verification gap — migration SQL is untestable in CI

`API.Tests/CustomWebApplicationFactory.cs:88-89` builds the schema with
`EnsureCreated()` from the **model** and then calls `MarkAllMigrationsAsApplied`,
so the host's `MigrateAsync()` is a no-op. **No migration SQL ever executes in the
test suite**, and the SQL is PostgreSQL-specific (`translate`, `regexp_replace`,
`to_regclass`, `uuid::text`) so it could not run on SQLite anyway.

Mandatory manual procedure, recorded in the PR before merge:

```powershell
# 0. Snapshot a dev DB that contains BOTH seeded and hand-created players.
pg_dump -Fc -f pre-rebackfill.dump club12_dev

# 1. Apply
dotnet ef database update --context ApplicationDBContext `
  --project Club12-Backend/Infrastructure --startup-project Club12-Backend/API
```

Assertions after step 1 (all must hold):

1. `SELECT count(*) FROM "Club12"."Players" WHERE "Slug" ~ '[0-9]{8}';` → `0` (no DNI leaked).
2. `SELECT count(*) - count(DISTINCT "Slug") FROM "Club12"."Players";` → `0`.
3. `SELECT count(*) FROM "Club12"."Players" WHERE "Slug" LIKE '\_\_tmp\_%';` → `0` (phase 2 completed).
4. `SELECT count(*) FROM "Club12"."Players" WHERE "Slug" !~ '^[a-z0-9]+(-[a-z0-9]+)*$';` → `0`.
5. Row counts match between `Players` and `PlayerSlugBackup_20260829`.
6. Browser: admin → Jugadores → "Ver" loads (no 404); a sanction link from a match page still loads.

```powershell
# 2. Rollback proof — REQUIRED, this is the artifact the reversibility decision rests on.
dotnet ef database update <PreviousMigrationName> --context ApplicationDBContext ...
```

Assertion after step 2: `Players."Slug"` equals `pre-rebackfill.dump` row-for-row
(`SELECT "Id","Slug" FROM "Club12"."Players" ORDER BY "Id"` diffed against the
restored snapshot) — **zero differences**. Then re-apply and reseed
(`DataMaintenanceService` regenerate) for normal dev use.

## Threat Matrix

| Boundary | Applicable? | Expected safe behaviour | RED test |
|---|---|---|---|
| Routing | **Applicable** | `admin/{idOrSlug}` matches only 4-segment `api/players/admin/*`; never shadows or is shadowed by `{idOrSlug}`; `[Authorize(AdminOrOwner)]` still gates it | Admin-route-not-shadowed test; 401/403 slug-form gating tests |
| Route-parameter injection | **Applicable** | `idOrSlug` reaches EF as a parameterized `WHERE "Slug" = $1` (`FindAsync` expression) — no string concatenation, no SQL injection surface. Over-long / punctuation-laden input simply misses the index and 404s | Unknown-slug → 404 test covers the non-matching path |
| Shell / subprocess | N/A | No process is spawned by this change | — |
| VCS/PR automation | N/A | No automation added | — |
| Executable-file classification | N/A | No file-type handling | — |
| Process integration | N/A | Single in-process ASP.NET request path | — |

Migration SQL is a *data* boundary, not a threat boundary: it takes no user input,
and every literal is compile-time constant.

## Migration / Rollout

- **Part 1**: no data or schema state. Deploy = deploy. Rollback = revert the commit.
- **Part 2**: apply the migration, then reseed dev. Existing slug URLs change; the
  only consumer is the admin-only detail route, so the blast radius is bookmarks
  inside the panel. Rollback = `dotnet ef database update <previous>` (restores
  prior slugs from the ledger) then revert the helper/seed/service commits.
- Part 1 stays functional throughout every Part 2 state, because it accepts
  whatever string is stored.

## Review Budget

Estimated **authored** changed lines (`additions + deletions`):

| Part | Backend src | Migration `.cs` | Tests (BE) | Tests (FE) | Authored total |
|---|---|---|---|---|---|
| 1 | ~12 | — | ~110 | ~170 | **~290** |
| 2 | ~35 | ~85 | ~90 | — | **~210** |

Plus a generated `.Designer.cs` of roughly 2,000–2,500 lines for Part 2. It is
EF-generated, so it is excluded from the authored-risk count but **not** from the
diff a human opens.

- **Against the 400-line default budget**: `400-line budget risk: High`.
- **Against the 800-line budget named for this change**: ~500 authored lines fits,
  but the generated Designer file makes the rendered diff ~2,700 lines.

**Recommendation: chained PRs.** PR #1 (tracker → `develop`) carries Part 1 alone:
~290 lines, no migration, no generated file, and it independently fixes the
user-visible 404. PR #2 targets PR #1's branch and carries Part 2 including the
migration, so the reviewer sees the generated Designer file in isolation from the
behavioural fix. `sdd-tasks` must record `Chained PRs recommended: Yes`.

## Open Questions — RESOLVED (2026-08-29)

- [x] Dev DB is **seed-only**. The manual verification (Phase 7) inserts a
      synthetic duplicate-surname player pair before running the assertions so
      the `-2` collision path is exercised.
- [x] Ledger table `PlayerSlugBackup_20260829` stays **permanent** for now. No
      follow-up drop migration; revisit after the release is confirmed.
- Delivery: single PR under `size:exception` (chained-PR split declined).
