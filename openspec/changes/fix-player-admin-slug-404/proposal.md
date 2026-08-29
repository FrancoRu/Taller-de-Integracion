# Proposal: Admin Player Detail Accepts Slug + Unified Player Slug Format

**Touches**: Part 1 = backend (controller) + frontend tests only. Part 2 = backend (entity/service/seed) + EF migration.

## Intent

The admin players list "Ver" button navigates with the stored `Player.Slug`, but `GET /api/players/admin/{id:guid}` only accepts a GUID. The route constraint rejects the slug, so MVC returns a routing-level 404 before any controller code runs — the admin player detail page is broken for every player. The public route `GET /api/players/{idOrSlug}` already accepts both.

The same investigation exposed a latent divergence: player slugs are generated in two incompatible formats — `apellido-nombre[-segundo]` (`CreatePlayerAsync` + the shipped backfill migration) versus `apellido-nombre-dni` (`SampleTournamentBuilder`). Seeded and real data disagree, so URLs are unpredictable and the seed leaks DNI into public URLs.

## Scope

### In Scope — Part 1 (fix the 404)

- `PlayerController.GetPlayerByIdCompleteDataAsync`: `[HttpGet("admin/{id:guid}")](Guid id)` → `[HttpGet("admin/{idOrSlug}")](string idOrSlug)`.
- Resolve via the existing, already-tested `PlayerService.GetPlayerByIdOrSlugAsync`; `AdminPlayerResponse` mapping unchanged.
- GUID form MUST keep working (`matchPage.tsx:513` navigates with a GUID).
- Lookup stays **exact/ordinal** against `Player.Slug` — same semantics as the public route.
- Backend tests (slug 200, GUID 200, unknown 404, `admin` literal wins over `{idOrSlug}`, authorization gating green) + frontend characterization tests. No frontend code change.

### In Scope — Part 2 (unify the slug format)

- Canonical format: **`apellido-nombre[-segundo]`, no DNI**, with `-2..-N` collision suffixes.
- Shared player slug-source helper (last/first/second name) used by `CreatePlayerAsync` and the seed.
- Player `SlugRegistry` in the seed (mirrors Division/Stage) so the small seed name pool cannot violate `IX_Players_Slug`.
- EF migration re-backfilling existing `Player.Slug` to the canonical format (`--context ApplicationDBContext`; `.cs` + `.Designer.cs` + snapshot so `EveryApplicationMigration_IsRegistered_WithMigrationAttribute` stays green).
- Dev reseed after the migration.

**Rationale for dropping DNI**: `CreatePlayerAsync` and the already-shipped backfill are the production behavior; only the seed disagrees. Keeping DNI would require changing shipped create+backfill logic, would publish a national ID in every URL, and would only exist to paper over collisions the registry solves properly.

### Out of Scope (Non-Goals)

- Case-insensitive or accent-normalizing lookup (wrong-case slug still 404s — pre-existing, both routes).
- Any change to public route `GET /api/players/{idOrSlug}` behavior.
- New slug formats or slug generation for other entities.
- HU-99 `Club` parent entity; team/tournament season scoping.

## Capabilities

### New Capabilities

- `player-slug-identity`: how a player is addressed by id-or-slug on public and admin routes, and the single canonical `Player.Slug` format across create, seed, and backfill.

### Modified Capabilities

- None.

## Approach

Part 1 is exploration Option 1: reuse `GetPlayerByIdOrSlugAsync`, minimal diff, brings player-admin in line with blogPost/club/tournament/division/match/team which are already id-or-slug on both ends. Route disambiguation is safe (literal segment `admin` outranks `{idOrSlug}`) and gets an explicit test.

Part 2 is exploration Option 3: one slug-source helper as the single truth, a seed-side registry for uniqueness, and a re-backfill migration to converge existing rows. Part 1 works with either format, so Part 1 lands independently of Part 2.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `API/Controllers/PlayerController.cs:131-146` | Modified | Route + parameter widened to `idOrSlug` |
| `Application/Services/PlayerService.cs` | Modified (P2) | Create uses shared slug-source helper |
| Player slug-source helper | New (P2) | Shared by create + seed |
| `Infrastructure/Persistance/SampleTournamentBuilder.cs:320-327` | Modified (P2) | Drop DNI, use helper + `SlugRegistry` |
| New EF migration (`ApplicationDBContext`) | New (P2) | Re-backfill `Player.Slug` |
| `API.Tests/PlayerSlugTests.cs`, `AuthorizationGatingTests.cs`, `EntitySlugLookupTests.cs` | Modified | Admin id-or-slug coverage |
| `Club12-WebClient` PlayersPage/PlayerPage tests | New | Characterization only, no source change |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Migration backfill SQL is **not** exercised by tests — API.Tests use SQLite `EnsureCreated()` from the model, not migrations | High | Documented verification gap; mandatory manual dev-DB check step (apply migration → assert no `IX_Players_Slug` violation, spot-check rows) before merge |
| Re-backfill changes existing slugs → old bookmarked/shared player URLs 404 | Med | Accepted; admin-only surface, no public player-detail route exists |
| Backfill collides on the unique index for real duplicate names | Med | Backfill MUST apply deterministic `-2..-N` suffixes, same rule as create |
| `admin/{idOrSlug}` ambiguity with `{idOrSlug}` | Low | Literal-segment precedence; explicit route test |
| Part 2 inflates the PR beyond review budget | Med | Ship Part 1 and Part 2 as separate slices if the tasks-phase forecast is high |
| Exact-match lookup surprises a hand-typed uppercase URL | Low | Explicit non-goal; identical to public route today |

## Rollback Plan

- **Part 1**: revert the `PlayerController` commit. Route returns to `admin/{id:guid}`; no data or schema state involved.
- **Part 2**: `dotnet ef migrations remove` (if unapplied) or apply the down migration, which restores the pre-change slug values; then revert the helper/seed/service commits and reseed the dev DB. The down migration MUST restore slugs, not drop the column. Part 1 remains functional throughout rollback of Part 2, since it accepts whatever format is stored.

## Dependencies

- Existing `PlayerService.GetPlayerByIdOrSlugAsync` and `SlugGenerator` (NFD → strip accents → lowercase → `[^a-z0-9]+` → `-`).
- Division/Stage `SlugRegistry` as the reference implementation.
- Integration branch is `develop`, not `main`. Strict TDD active.

## Success Criteria

- [ ] `GET /api/players/admin/{slug}` returns 200 with `AdminPlayerResponse` for an existing player.
- [ ] `GET /api/players/admin/{guid}` still returns 200; `matchPage` sanction navigation unaffected.
- [ ] Unknown id-or-slug returns 404 with `ProblemDetails` shape; authorization gating tests stay green.
- [ ] Admin "Ver" navigates and loads without a 404 against a seeded DB.
- [ ] Create, seed, and backfill all produce `apellido-nombre[-segundo]` with registry-assigned suffixes; no `IX_Players_Slug` violation after migration + reseed.
- [ ] Backend and frontend suites green; manual dev-DB migration check recorded.

## Proposal question round

Scope was approved by the user before this phase; no user turn is available here. These decisions are documented as assumptions and need a correction only if the user disagrees:

1. Canonical format recommended as `apellido-nombre[-segundo]` (DNI dropped) — see rationale above.
2. Re-backfill deliberately breaks previously shared slug URLs; treated as acceptable because the only consumer is the admin-only detail route.
3. The down migration is expected to restore prior slug values, which requires the migration to be written as a reversible transform (or to snapshot old values) — flagged for the design phase.
4. Part 1 and Part 2 may be split into chained PRs if the tasks-phase 400-line forecast is Medium/High.
