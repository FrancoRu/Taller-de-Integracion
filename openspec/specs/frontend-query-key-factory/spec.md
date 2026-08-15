# Frontend Query-Key Factory Specification

## Purpose

Define behavior-preservation guarantees for extracting inline TanStack Query
key literals (e.g. `['blogPost','byId',id]`) into per-module factory
functions across the 12 `Club12-WebClient/src/modules/{domain}` contexts.
This is a pure refactor: the single real risk is a factory emitting a
different tuple than the literal it replaces, silently breaking cache reads
or invalidations (stale cache, phantom refetches).

## Requirements

### Requirement: Factory Tuple Byte-Identity

Each per-module query-key factory (`modules/{domain}/queryKeys.ts`) MUST
produce tuples that are deep-equal, in value and structure, to the inline
literal array it replaces at every call site. No key segment MAY be
reordered, renamed, added, or omitted.

#### Scenario: By-id key matches prior literal

- GIVEN the `blogPost` module defines `blogPostKeys.byId(id)`
- WHEN the factory is called with a `GUID` id
- THEN it MUST return `['blogPost', 'byId', id]`
- AND this MUST deep-equal the literal previously passed to
  `queryClient.setQueryData(['blogPost', 'byId', id], response)` and
  `fetchQuery({ queryKey: ['blogPost', 'byId', id], ... })`

#### Scenario: List key with filter matches prior literal

- GIVEN the `blogPost` module defines `blogPostKeys.list(filter)`
- WHEN called with a `GetBlogPostsFilteredRequest` filter object
- THEN it MUST return `['blogPost', 'list', filter]`
- AND MUST deep-equal the literal previously used in
  `fetchQuery({ queryKey: ['blogPost', 'list', filter], ... })`

#### Scenario: List key without filter matches prior literal

- GIVEN `blogPostKeys.list()` is called with no arguments (invalidation-only
  use)
- WHEN evaluated
- THEN it MUST return exactly `['blogPost', 'list']`, with no trailing
  `undefined` slot
- AND MUST deep-equal the literal previously passed to
  `invalidateQueries({ queryKey: ['blogPost', 'list'] })`

### Requirement: Cache Behavior Preserved Across Representative Patterns

For each representative query pattern (list query, by-id query, mutation
invalidation), switching a call site from an inline literal to the matching
factory call MUST preserve the exact cache read, write, invalidation, and
refetch behavior observed before the change. Verification MAY use one
concrete example per pattern rather than covering all 12 modules
individually.

#### Scenario: List query pattern preserves refetch trigger

- GIVEN a component fetches via
  `fetchQuery({ queryKey: blogPostKeys.list(filter), ... })` (post-migration)
- WHEN a mutation invalidates via
  `invalidateQueries({ queryKey: blogPostKeys.list() })`
- THEN the cached list query for that `filter` MUST be marked stale and
  refetch under the identical trigger conditions as the pre-migration inline
  call

#### Scenario: By-id query pattern preserves cache identity

- GIVEN a response was cached via
  `setQueryData(blogPostKeys.byId(id), response)`
- WHEN `getBlogPostsById(id)` reads via
  `fetchQuery({ queryKey: blogPostKeys.byId(id), ... })`
- THEN the cached response MUST be returned without a phantom refetch
- AND `removeQueries({ queryKey: blogPostKeys.byId(id) })` MUST evict exactly
  that entry, matching the prior inline `removeQueries` call

#### Scenario: Mutation invalidation pattern preserves scope

- GIVEN `putBlogPostById` succeeds and invalidates via
  `invalidateQueries({ queryKey: blogPostKeys.list() })`
- WHEN this fires
- THEN every cached list query for the `blogPost` module (any filter
  variant) MUST be marked stale, matching the prior partial-match
  invalidation behavior of `['blogPost', 'list']`
- AND queries under `blogPostKeys.byId(id)` MUST NOT be invalidated by this
  call

### Requirement: No New Query Behavior Introduced

The migration MUST NOT change query options, add new queries/mutations, or
touch files outside the query-key swap scope.

#### Scenario: Query options unchanged

- GIVEN a `useQuery`/`fetchQuery` call site with existing `queryFn`,
  `staleTime`, `gcTime`, `enabled`, or `retry` options
- WHEN its query key literal is replaced by a factory call
- THEN no such option's value MAY change

#### Scenario: No out-of-scope files touched

- GIVEN the 12 modules with inline query-key literals
- WHEN the migration is applied
- THEN only `modules/{domain}/queryKeys.ts` (new), the matching
  `context/*.context.tsx` call sites, and new equivalence tests MAY be
  modified
- AND colors, `axiosUtils.ts`, and i18n files (`batch2b` scope) MUST remain
  untouched
