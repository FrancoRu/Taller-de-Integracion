# Player Slug Identity — Delta Spec

## Purpose

Define how a player is addressed by id-or-slug on the public and admin routes, and the single canonical `Player.Slug` format shared by create, seed, and re-backfill. New capability; no prior spec.

## ADDED Requirements

### Requirement: Admin Player-Detail Route Accepts Id or Slug

`GET /api/players/admin/{idOrSlug}` MUST resolve a player by either a GUID or an exact `Player.Slug`, returning 200 with the `AdminPlayerResponse` body. The GUID form MUST keep working unchanged.

#### Scenario: Resolve by GUID
- GIVEN a player exists with id `G`
- WHEN a client sends `GET /api/players/admin/G`
- THEN the response is 200 with `AdminPlayerResponse` for that player

#### Scenario: Resolve by exact slug
- GIVEN a player exists with slug `lopez-carlos`
- WHEN a client sends `GET /api/players/admin/lopez-carlos`
- THEN the response is 200 with `AdminPlayerResponse` for that player

### Requirement: Exact-Match Slug Lookup (No Normalization)

Slug lookup on the admin route MUST be exact/ordinal against the stored `Player.Slug`, identical to the public route. The system MUST NOT case-fold, accent-normalize, or otherwise transform the request value before matching.

#### Scenario: Wrong-case slug is not found
- GIVEN a player exists with slug `lopez-carlos`
- WHEN a client sends `GET /api/players/admin/Lopez-Carlos`
- THEN the response is 404 with a `ProblemDetails`-shaped body

### Requirement: Unknown Identifier Returns 404 ProblemDetails

When no player matches the supplied GUID or slug, the admin route MUST return 404 with a `ProblemDetails`-shaped body, not a bare string and not a routing-level 404.

#### Scenario: Unknown slug
- GIVEN no player has slug `no-such-player`
- WHEN a client sends `GET /api/players/admin/no-such-player`
- THEN the response is 404 with a `ProblemDetails` body

### Requirement: Route Disambiguation Favors the Literal Segment

A request to `GET /api/players/admin/{value}` MUST bind to the admin action, never to the public `{idOrSlug}` action. The literal `admin` segment MUST outrank the parameter route.

#### Scenario: admin path binds to admin action
- GIVEN both the public `{idOrSlug}` and admin `admin/{idOrSlug}` routes are registered
- WHEN a client sends `GET /api/players/admin/lopez-carlos`
- THEN the admin action handles the request with its authorization enforced

### Requirement: Admin Authorization Preserved

The admin route MUST keep its `AdminOrOwner` authorization. Widening the parameter from GUID to string MUST NOT weaken the gate.

#### Scenario: Unauthenticated request rejected
- GIVEN no authenticated user
- WHEN a client sends `GET /api/players/admin/lopez-carlos`
- THEN the response is 401, or 403 for a non-admin non-owner, never 200

### Requirement: Public Route Behavior Unchanged

`GET /api/players/{idOrSlug}` MUST retain its current semantics: GUID or exact-case slug, unknown → 404. This change MUST NOT alter the public route's template, parameter, or lookup.

#### Scenario: Public route still resolves by id and slug
- GIVEN a player exists with id `G` and slug `lopez-carlos`
- WHEN a client sends `GET /api/players/G` or `GET /api/players/lopez-carlos`
- THEN each returns 200 with the public player response
- AND `GET /api/players/Lopez-Carlos` returns 404 as before

### Requirement: Canonical Player Slug Format

`Player.Slug` MUST be derived only from the player's names as `apellido-nombre[-segundo]` (last name, first name, optional second given name). The DNI/document number MUST NOT appear. Generation MUST NFD-normalize, strip diacritics, lowercase, replace each `[^a-z0-9]+` run with `-`, and trim leading/trailing `-`. On collision with an existing slug, a deterministic suffix `-2`, `-3`, … `-N` MUST be appended.

#### Scenario: Slug from names without DNI
- GIVEN a player "Carlos López" with document `30000001`
- WHEN the slug is generated
- THEN the slug is `lopez-carlos` and contains no digits from the document

#### Scenario: Collision suffix
- GIVEN slug `lopez-carlos` already exists
- WHEN a second "Carlos López" slug is generated
- THEN the new slug is `lopez-carlos-2`

### Requirement: Consistent Slug Generation Across Create, Seed, and Backfill

The canonical format MUST be produced identically by `CreatePlayerAsync`, the sample-data seed (`SampleTournamentBuilder`), and the re-backfill migration, sharing one name-source rule.

#### Scenario: Seed and create agree
- GIVEN the same player names
- WHEN one player is seeded and an equivalent player is created via the API
- THEN both slugs follow `apellido-nombre[-segundo]` with the same normalization

### Requirement: Seed Slug Uniqueness

The seed MUST use a player `SlugRegistry` (mirroring the Division/Stage registries) that guarantees each seeded `Player.Slug` is unique before insert, so seeding never violates `IX_Players_Slug` when seed names repeat.

#### Scenario: Repeated seed names get distinct slugs
- GIVEN the seed produces two players whose names normalize to `lopez-carlos`
- WHEN the seed runs
- THEN the registry assigns `lopez-carlos` and `lopez-carlos-2` and seeding completes without a unique-index violation

### Requirement: Reversible Slug Re-Backfill Migration

The EF migration MUST convert every existing `Player.Slug` to the canonical format without violating `IX_Players_Slug`, applying the same deterministic `-2..-N` collision rule. The down migration MUST restore the pre-migration slug values (reversible transform or stored snapshot), not drop the column.

#### Scenario: Up converges existing rows
- GIVEN existing player rows with mixed `apellido-nombre-dni` and `apellido-nombre` slugs
- WHEN the migration `Up` runs
- THEN every row has a canonical `apellido-nombre[-segundo]` slug and no two rows share a slug

#### Scenario: Down restores prior values
- GIVEN the migration `Up` has been applied
- WHEN the migration `Down` runs
- THEN each player's `Slug` equals its exact pre-migration value

## Non-Goals

- Case-insensitive or accent-normalizing lookup on either route; wrong-case slugs remain 404.
- Any change to the public route `GET /api/players/{idOrSlug}` matching semantics.
- Slug format or generation changes for entities other than `Player`; the HU-99 `Club` entity is untouched.
