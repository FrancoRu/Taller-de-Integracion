# Design: Cleanup — Query-Key Factories (Batch 2, Frontend)

## Technical Approach

Extract inline TanStack Query key literals into one pure-function factory per
module (`modules/{domain}/queryKeys.ts`), then swap every inline array at each
call site for the matching factory call. Emitted tuples stay byte-identical so
cache reads, writes, invalidations, and refetches are unchanged. Verified by
narrow Vitest unit tests on the factory functions plus `npm run test`. Realizes
spec `frontend-query-key-factory`; pure refactor, no behavior change.

## Scope (verified, not assumed)

Query keys live in `modules/{domain}/context/{domain}.context.tsx` (NOT the
`.hook.ts` files — those only re-export context). 12 modules actually contain
key literals: `auth, blogPost, division, match, player, playerSanction,
playerStatistic, scorer, stage, team, user, venue`. `tournament` and `error`
have no query keys and are excluded.

Observed key shapes (real, per module):

| Pattern | Literal | Modules |
|---------|---------|---------|
| list root | `[ns,'list']` | most CRUD modules |
| filtered list | `[ns,'list',filter]` | blogPost, division, match, player, playerSanction, playerStatistic, stage, team, user |
| by id | `[ns,'byId',id]` | most CRUD modules |
| by id + arg | `['player','byId',id,isAdministrative]` | player |
| bare root | `['playerStatistic']` | playerStatistic (invalidate-all) |
| named sub-key | `['division','top-scorers',id]` | division |
| named sub-key | `['scorer','byTeam',filter]`, `['scorer','byPlayer',filter]` | scorer |
| singleton | `['auth','has-token']` | auth |

## Architecture Decisions

### Decision: Factory shape mirrors literal segments (spec naming)

**Choice**: Export a `const` object `{ns}Keys` whose members mirror the actual
string segments the module uses (`all`, `list`, `byId`, and module-specific
`topScorers`, `byTeam`, `byPlayer`, `hasToken`). Member names match the spec
(`blogPostKeys.byId(id)`, `blogPostKeys.list(filter)`).
**Alternatives**: TanStack's generic hierarchical `lists()/details()` naming;
one global key registry.
**Rationale**: Segment-mirroring names keep each factory call trivially
traceable to the literal it replaced, easing byte-identity review. A single
global file was rejected by the proposal (per-module locality, matches existing
module structure). Only modules that use a segment get that member — no dead
generic surface.

### Decision: `list` takes an optional filter, no trailing `undefined`

**Choice**:
`list: (filter?: F) => filter === undefined ? [ns,'list'] : [ns,'list',filter]`.
**Alternatives**: separate `list()` and `listFiltered(f)`; always append filter.
**Rationale**: Spec requires `list()` → exactly `[ns,'list']` (invalidation) and
`list(filter)` → `[ns,'list',filter]` (fetch) from one member. Always-append
would emit `[ns,'list',undefined]`, a different tuple → cache miss.

### Decision: `as const` for types only

**Choice**: annotate returns `as const` (readonly tuples).
**Rationale**: TanStack v5 keys accept readonly arrays; `as const` is
compile-time only — runtime array bytes are identical, preserving cache identity.

## Data Flow

    context.tsx call site ──> {ns}Keys.member(args) ──> tuple literal
         (useQuery / fetchQuery / invalidateQueries / setQueryData /
          removeQueries)  ── same tuple ──> QueryClient cache (unchanged)

`setQueryData` uses the tuple as a positional first arg; `fetchQuery`,
`invalidateQueries`, `removeQueries` use it under `{ queryKey }`. Both swap in
place.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `modules/{domain}/queryKeys.ts` ×12 | Create | Exports `{ns}Keys` pure factory |
| `modules/{domain}/queryKeys.test.ts` ×12 | Create | Equivalence unit tests |
| `modules/{domain}/context/{domain}.context.tsx` ×12 | Modify | Replace inline key arrays with factory calls; add one import |

Out of scope (batch2b), MUST stay untouched: brand colors, `axiosUtils.ts`,
i18n files.

## Interfaces / Contracts

Representative factory (`modules/blogPost/queryKeys.ts`):

```ts
export const blogPostKeys = {
  all: ['blogPost'] as const,
  list: (filter?: GetBlogPostsFilteredRequest) =>
    filter === undefined
      ? (['blogPost', 'list'] as const)
      : (['blogPost', 'list', filter] as const),
  byId: (id: GUID) => ['blogPost', 'byId', id] as const,
};
```

Module-specific members added only where used: `player.byId(id, isAdministrative?)`,
`divisionKeys.topScorers(id)`, `scorerKeys.byTeam(filter)/byPlayer(filter)`,
`authKeys.hasToken()`, `playerStatisticKeys.all`.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Each factory member returns the exact prior literal | Vitest `toEqual` against the hand-written literal per member; assert `list()` has no trailing `undefined`; colocated `queryKeys.test.ts`, no render/QueryClient — cheap/fast |
| Unit | player 4-arg / division / scorer / auth variants | Explicit cases for non-standard shapes |
| Suite | No regression | `npm run test` (existing Vitest, globals + jsdom) |

Cache-behavior scenarios from the spec are covered structurally: identical
tuples guarantee identical cache identity, so byte-identity unit tests are the
proportional proof. No new integration/E2E needed.

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file
classification, or process-integration boundary. Pure in-repo TS refactor.

## Migration / Rollout

No migration. Additive files + in-place literal swaps; single PR; `git revert`
rollback. No data/schema/route/config change.

## Open Questions

- [ ] None blocking. Factory location/shape (open Q2 in proposal) resolved:
  per-module `queryKeys.ts` with segment-mirroring `{ns}Keys` object.
