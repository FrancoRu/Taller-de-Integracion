# Exploration — fix-player-admin-slug-404

> Phase: sdd-explore · Store: hybrid (Engram topic `sdd/fix-player-admin-slug-404/explore` id 171 + this file)
> Read-only investigation. No production code changed.

## Bug report

Admin panel, "Ver" button on a player row. Navigation triggers:

```
[15:50:55 INF] HTTP GET /api/players/admin/lopez-carlos-30000001 responded 404 in 0.7550 ms
```

Initial hypothesis (malformed slug, wrong field order) is **wrong**. `lopez-carlos-30000001`
is the player's real, persisted `Slug` value.

## Current state

### Frontend chain (admin "Ver" button)

- `Club12-WebClient/src/views/player/PlayersPage.tsx:286` — `handleView` → `navigate(APP_ROUTES.panelPlayer.build(row.slug))`. Uses the **stored** slug from the list response (`IPlayerResponse.slug`); nothing derived client-side.
- `src/modules/core/constants/appRoutes.ts:47-50` — `panelPlayer` = `/panel/jugadores/:playerId`.
- `src/App.tsx:97-101` — mounts `<PlayerPage/>` for Admin/Owner only.
- `src/views/player/PlayerPage.tsx:40` — reads `playerId` param (the slug); `:64` `isAdministrative = role !== Guest` (always true here); `:109` calls `getPlayerById(param, true)`.
- `src/modules/player/service/player.service.ts:44-47` — builds `GET ${routes.players}/admin/${idOrSlug}` → `GET /api/players/admin/lopez-carlos-30000001`.

### Backend

- `Club12-Backend/API/Controllers/PlayerController.cs:107-122` — PUBLIC `[HttpGet("{idOrSlug}")]` → `PlayerService.GetPlayerByIdOrSlugAsync` (accepts GUID **or** slug, already tested).
- `PlayerController.cs:131-146` — ADMIN `[Authorize(AdminOrOwner)] [HttpGet("admin/{id:guid}")] GetPlayerByIdCompleteDataAsync(Guid id)` → `GetPlayerByIdAsync(Guid)`. **GUID only.**
- `Club12-Backend/Application/Services/PlayerService.cs:58-67` — `GetPlayerByIdOrSlugAsync(string)` already exists (`Guid.TryParse` else `FindAsync(p => p.Slug == idOrSlug)`); the admin action does not call it.

### `Player.Slug`

Persisted, unique-indexed column. Migration `20260828003816_AddSlugToDivisionStageVenuePlayer.cs`
(`varchar(220)`, NOT NULL, `IX_Players_Slug`). `Domain/Entities/Models/Player.cs:21,24-27`.
`AdminPlayerResponse : PublicPlayerResponse` inherits `Slug`, so the admin list returns it — that is `row.slug`.

### Slug generation — three paths, two formats

| Path | Source | Format | DNI |
|---|---|---|---|
| Create — `PlayerService.CreatePlayerAsync:37-39` | `player.FullName` via `GenerateUniqueSlugAsync` | `apellido-nombre[-segundo]`, `-2/-3` on collision | no |
| DB backfill — migration `20260828003816` SQL lines 133-163 | `concat(LastName,' ',FirstName,secondName)` | same `apellido-nombre` | no |
| **Seed — `Infrastructure/Persistance/SampleTournamentBuilder.cs:327`** | `$"{lastName} {firstName} {documentNumber}"` | **`apellido-nombre-dni`** e.g. `lopez-carlos-30000001` | **yes** |

Seed DNI = `30000000 + playerCounter` (`SampleTournamentBuilder.cs:320`), 8 players/team.
The seed has **no player `SlugRegistry`** (unlike Division/Stage), so appending the DNI is how it
avoids collisions on the unique index. `SlugGenerator.GenerateSlug`: NFD normalize, strip accents,
lowercase, `[^a-z0-9]+`→`-`, trim `-`.

## The mismatch (why `lopez-carlos-30000001` → 404)

1. `PlayersPage.handleView` builds `/panel/jugadores/lopez-carlos-30000001` from `row.slug`.
2. `PlayerPage` (admin-only) calls `getPlayerById("lopez-carlos-30000001", true)`.
3. Service issues `GET /api/players/admin/lopez-carlos-30000001`.
4. `PlayerController.cs:132` route template is `admin/{id:guid}`. The `:guid` constraint rejects the
   slug; no other route matches `api/players/admin/*` → **routing-level 404**, before any
   controller/service code. It is **not** a "player not found" response.

The slug is **not malformed** — it is the real stored value. A freshly created (non-seed) player
would 404 identically because its slug `lopez-carlos` is still not a GUID. The seed-vs-production
slug format divergence is a separate latent issue, not the 404 cause.

## Other call sites

- `Club12-WebClient/src/views/match/matchPage.tsx:513` — `navigate(APP_ROUTES.panelPlayer.build(sanction.playerId))` passes a **GUID**; works today. Any fix MUST keep the GUID form working.
- No public player-detail route/view exists — `panelPlayer` is the only one.
- Sibling routes already accept id-or-slug on both ends: `blogPost`, `panelClub`, tournament/division/match/team `*ByIdOrSlug`. Player-admin is the outlier.

## Existing tests

- `Club12-Backend/API.Tests/PlayerSlugTests.cs` — create-slug-from-FullName, duplicate `-2` suffix, PUBLIC `GET api/players/{idOrSlug}` by id and slug + unknown 404. No admin-route slug coverage.
- `API.Tests/AuthorizationGatingTests.cs:34,47,63` — `api/players/admin/{Guid.NewGuid()}` → 401/403/404(Admin). GUID form only.
- `API.Tests/EntitySlugLookupTests.cs` — Team/Tournament/Match id-or-slug (Player excluded, has its own file).
- Frontend: **no** `PlayerPage.test.tsx` / `PlayersPage.test.tsx`.
- Harness gotcha (`Docs/HANDOFF.md:97`): API.Tests use SQLite `EnsureCreated()` from the model, not
  migrations — backfill SQL is never exercised. Strict TDD is active.

## Approaches

### 1. Backend — admin endpoint accepts id-or-slug (mirror the public route). RECOMMENDED

`PlayerController.GetPlayerByIdCompleteDataAsync`: `[HttpGet("admin/{id:guid}")](Guid id)` →
`[HttpGet("admin/{idOrSlug}")](string idOrSlug)` + resolve via the existing
`playerService.GetPlayerByIdOrSlugAsync(idOrSlug)`, then map to `AdminPlayerResponse` (mapping already
works on the GUID path).

- **Pros**: minimal diff; reuses an already-tested service method; consistent with the public route
  and every other entity; frontend untouched; both callers work (GUID from `matchPage`, slug from
  `PlayersPage`); fixes seed **and** real data; no migration; keeps readable admin URLs; matches the
  team's recent "navegaciones admin por slug" work.
- **Cons**: must verify route disambiguation `admin/{idOrSlug}` vs `{idOrSlug}` (safe — the literal
  `admin` segment wins; add a test).
- **Effort**: Low.

### 2. Frontend — navigate by GUID in the admin panel

`PlayersPage.handleView` → `APP_ROUTES.panelPlayer.build(row.id)`.

- **Pros**: low effort; no backend change; opaque GUID URLs acceptable for a private panel.
- **Cons**: loses readable admin URLs; leaves dead slug-handling paths in `PlayerPage`; contradicts
  the team's slug-navigation direction; does not fix bookmarked/shared slug URLs; backend admin route
  stays inconsistent.
- **Effort**: Low.

### 3. Option 1 + align seed slug format

Shared last/first/second helper used by entity/service/seed, a player `SlugRegistry` like
Division/Stage, and a re-backfill migration.

- **Pros**: removes the latent seed-vs-production divergence; one slug format everywhere.
- **Cons**: much larger; migration + dev reseed; the seed's small name pool genuinely collides so a
  registry is mandatory and seed slugs still get `-2..-N`; migration backfill unverifiable in the
  SQLite harness; not required to fix the 404.
- **Effort**: Medium-High.

## Recommendation

**Option 1.** Smallest safe change, reuses tested code, restores cross-entity consistency, fixes both
seed and real data, no migration, no frontend code change. Add tests (strict TDD):

- Backend: Admin `GET api/players/admin/{slug}` → 200 with the matching player; by GUID → 200;
  unknown slug → 404; keep `AuthorizationGatingTests` green.
- Frontend: `PlayersPage` "Ver" navigates to `/panel/jugadores/{slug}`; `PlayerPage` issues
  `GET /api/players/admin/{param}` (mock axios).

Treat option 3's seed-format alignment as a separate low-priority follow-up.

## Fix touches

**Backend primarily**: one controller action (`PlayerController.GetPlayerByIdCompleteDataAsync`) +
tests. No migration, no service change (method already exists). **Frontend**: no code change
required; add characterization tests only. Integration branch is **develop**, not `main`.

## Risks

- ASP.NET route disambiguation `admin/{idOrSlug}` vs `{idOrSlug}` — literal segment wins; cover with a test.
- `p.Slug == idOrSlug` is PostgreSQL case-sensitive — fine for real flows; hand-typed wrong-case URLs
  404 (pre-existing, same as the public route).
- SQLite test harness skips migrations, so option 3's backfill would be unverified.
