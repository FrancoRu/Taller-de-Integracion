# Blog List Ordering Specification

## Purpose

Define the default ordering of the paginated blog-post list served by
`GET /api/blog-posts` (consumed by the home "Últimas noticias" section, the
public `/blog` list and the admin Novedades list) when the caller supplies no
explicit sort parameter.

Scope is limited to behavior observable through
`BlogPostService.GetAllBlogPostsAsync` with xUnit + the SQLite in-memory
harness.

## Requirements

### Requirement: Newest-First Default Ordering by Creation Date

The paginated blog-post list MUST default to ordering by `BlogPost.DateCreated`
descending (most recently created first) when the request carries no explicit
`orderBy`. The first page MUST therefore contain the most recently created
posts.

A caller that supplies an explicit `orderBy` / `order` MUST still override
this default.

#### Scenario: DTO carries the newest-first default

- GIVEN a freshly constructed `GetBlogPostsFilteredRequest` with no properties
  assigned
- WHEN its `OrderBy` and `Order` are read
- THEN `OrderBy` equals `"DateCreated"`
- AND `Order` equals `SortOrder.Descending`

#### Scenario: Service returns posts newest-first

- GIVEN three published posts by one author created on 2026-01-10, 2026-03-01
  and 2026-02-05, inserted in that (non-chronological) order
- WHEN `GetAllBlogPostsAsync` is called with a filter that selects only those
  three and no explicit sort
- THEN the returned items appear in the order 2026-03-01, 2026-02-05,
  2026-01-10
- AND each item's `DateCreated` is less than or equal to the previous item's

#### Scenario: Explicit sort still wins

- GIVEN a `GetBlogPostsFilteredRequest` with `OrderBy = "DateCreated"` and
  `Order = SortOrder.Ascending`
- WHEN `GetAllBlogPostsAsync` is called
- THEN the returned items appear oldest-first

### Requirement: Published-Only Filter Unaffected by the Ordering Default

Changing the default ordering MUST NOT change which posts a public caller
sees. Drafts (HU-16) MUST still be excluded when `includeUnpublished` is
false.

#### Scenario: Draft still hidden from the public list

- GIVEN one published and one draft post by the same author
- WHEN `GetAllBlogPostsAsync` is called for that author without
  `includeUnpublished`
- THEN only the published post is returned
