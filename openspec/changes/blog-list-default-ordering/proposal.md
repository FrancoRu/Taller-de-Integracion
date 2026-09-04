# Proposal: Blog / Novedades List Defaults to Newest-First by Creation Date

**Touches**: Backend only (`Club12-Backend`, one Application DTO + tests). No frontend, no API surface, no schema, no migration.

## Intent

The home page's "Últimas noticias" section and the public `/blog` list (and
the admin Novedades list) all read `GET /api/blog-posts` and send **no sort
parameter**. `GetBlogPostsFilteredRequest` inherits `PaginatedFilterRequest`'s
defaults — `OrderBy = "DateCreated"`, `Order = Ascending` — so posts come back
**oldest first**.

The home section fetches only the first `LATEST_POSTS_COUNT` posts and labels
them "the freshest thing first", but it is actually showing the oldest posts.
`/blog` paginates from the oldest. None of these views sort client-side.

## Scope

### In Scope

- `GetBlogPostsFilteredRequest` gets a constructor that defaults the ordering
  to `OrderBy = DateCreated`, `Order = Descending`.
- Backend tests: the DTO default is `DateCreated` / `Descending`; the service
  returns posts ordered by `DateCreated` descending when no sort is supplied;
  the published-only filter (HU-16) still applies.

### Out of Scope (Non-Goals)

- Any frontend change. Every list already relies on the backend order.
- Wiring a server-side column sort into the admin DataGrid.
- A stable secondary tiebreaker for posts sharing the exact same
  `DateCreated` (`QueryableExtensions.SortBy` applies a single `OrderBy` with
  no `ThenBy`; changing that touches every paginated list).
- Changing `PaginatedFilterRequest`'s own defaults.

## Capabilities

### New Capabilities

- `blog-list-ordering`: the default ordering of the paginated blog-post list
  served by `GET /api/blog-posts` when the caller supplies no explicit sort.

### Modified Capabilities

- None.

## Approach

Override the two inherited ordering defaults in the blog filter DTO only —
identical to the fix already shipped for the sanctions list
(`sanction-list-default-ordering`). `[FromQuery]` model binding keeps a
property's initialised value when the query string omits it, so a caller that
never sends `orderBy` now gets `DateCreated` descending, while a caller that
does send one still wins.

`BlogPost.DateCreated` is inherited from `EntityBase` with no column rename,
so `SortBy`'s reflection-by-name resolves it directly (the base default
already relies on this).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/DTOs/BlogPosts/Request/GetBlogPostsFilteredRequest.cs` | Modified | Constructor sets `OrderBy = "DateCreated"`, `Order = SortOrder.Descending` |
| `API.Tests/BlogPostOrderingTests.cs` | New | DTO default + service ordering + published-filter regression |

The only production consumer of the DTO is
`BlogPostController` (`GET /api/blog-posts`), used by the home section, the
public `/blog` list and the admin Novedades list.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| An existing test asserts the old oldest-first order | Low | Verified: no blog test asserts list order |
| Same-`DateCreated` posts shuffle across pages | Low | `DateCreated` is a full timestamp; documented non-goal |
| A caller expected the inherited `Ascending` default | Low | Never intentional for this list; documented in the DTO |

## Rollback Plan

Revert the single DTO commit. No data, schema, or persisted state. The lists
return to oldest-first.

## Success Criteria

- [ ] `new GetBlogPostsFilteredRequest()` has `OrderBy == "DateCreated"` and `Order == SortOrder.Descending`.
- [ ] `GetAllBlogPostsAsync` with no sort supplied returns items in strictly non-increasing `DateCreated` order.
- [ ] The HU-16 published-only filter still applies for public callers.
- [ ] Backend suite green; `dotnet build` 0 warnings.
- [ ] Manual: home "Últimas noticias" and `/blog` show the most recent post first.
