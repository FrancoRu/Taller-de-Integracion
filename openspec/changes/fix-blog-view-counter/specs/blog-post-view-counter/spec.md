# Blog Post View Counter — New Capability Spec

## Purpose

Define the observable behavior of the blog-post read path with respect to the
`Views` counter and the detail-page rendering experience. A public read MUST
increment `Views` once per load regardless of whether the reader arrived through
in-app navigation (router state carries the post) or a direct URL; Admin/Owner
reads MUST NOT increment. No existing spec covers the blog-post read path, so
this is a full new capability spec, not a delta.

Scope is behavior-first and black-box: requirements are stated in terms of
observable API calls, the persisted counter value, and visible UI state. They do
not prescribe the client framework implementation.

## ADDED Requirements

### Requirement: In-App Read Increments the View Counter

A public reader (anonymous, or an authenticated user who is neither Admin nor
Owner) opening a published post through in-app navigation — whether from the
"Leer más" action on the Novedades list or a "Últimas noticias" card on the home
page, both of which pass the post object in router state — MUST cause exactly one
`GET /api/blogposts/{idOrSlug}` for that post, and that request MUST increment
the post's `Views` by exactly 1. No in-app entry point may pre-fetch the post
before navigating: the detail page is the single owner of that request.

#### Scenario: "Leer más" from the list counts one view

- GIVEN a published post with `Views = N` and a public reader on the post list
- WHEN the reader activates "Leer más" and the detail page is shown with the post supplied in router state
- THEN exactly one `GET /api/blogposts/{idOrSlug}` is issued for that post
- AND the post's persisted `Views` becomes `N + 1`

#### Scenario: "Últimas noticias" card on the home page counts one view

- GIVEN a published post with `Views = N` shown in the home page "Últimas noticias" section
- WHEN a public reader activates the card and the detail page is shown with the post supplied in router state
- THEN no request is issued before navigation, and exactly one `GET /api/blogposts/{idOrSlug}` is issued by the detail page
- AND the post's persisted `Views` becomes `N + 1`

### Requirement: Direct-URL Read Increments the View Counter

A public reader opening a published post by direct URL entry or a hard refresh —
no router state present — MUST also cause exactly one
`GET /api/blogposts/{idOrSlug}` that increments `Views` by exactly 1. This
behavior is unchanged by this capability.

#### Scenario: Direct URL open counts one view

- GIVEN a published post with `Views = N` and no router state
- WHEN a public reader loads the detail page by URL
- THEN exactly one `GET /api/blogposts/{idOrSlug}` is issued
- AND the post's persisted `Views` becomes `N + 1`

### Requirement: Admin and Owner Reads Do Not Increment

An Admin or Owner opening a post through any path (in-app navigation or direct
URL) MUST NOT change the post's `Views` value.

#### Scenario: Admin "Ver" does not count

- GIVEN a post with `Views = N` and an authenticated Admin or Owner
- WHEN the Admin opens the post detail through any path
- THEN the post's persisted `Views` remains `N`

### Requirement: Instant Render From Router-State Post

When a post is supplied in router state, the detail page MUST render that post's
content immediately. The page MUST NOT display a loading skeleton and MUST NOT
show a blank flash while the background request is in flight.

#### Scenario: No skeleton flash on the state-present path

- GIVEN a reader navigates to the detail page with the post in router state
- WHEN the page mounts and the background `GET` has not yet resolved
- THEN the post title and body are visible from the first render
- AND no loading skeleton or blank placeholder is shown

### Requirement: Fetched Server Copy Replaces the Displayed Post

When the background `GET /api/blogposts/{idOrSlug}` resolves successfully, the
displayed post MUST be replaced with the fetched server copy.

#### Scenario: Resolve swaps in server content

- GIVEN the detail page is showing the router-state post
- WHEN the background `GET` resolves with an updated post body
- THEN the page displays the fetched server copy in place of the router-state post

### Requirement: Silent Failure Keeps the Router-State Post

The background request MUST be issued in silent mode (`{ silent: true }`). If it
fails, the page MUST keep showing the router-state post and MUST NOT raise the
global blocking alert.

#### Scenario: Background GET fails

- GIVEN the detail page is showing the router-state post
- WHEN the background `GET` fails
- THEN the router-state post remains visible
- AND no global blocking alert is shown

Note: whether a `404` specifically (post deleted between list and open) should
additionally route to the not-found branch is deferred to the design phase; this
requirement covers the generic failure case only.

### Requirement: Every Qualifying Load Counts

The capability MUST NOT deduplicate views per session, per day, or per visitor.
Every qualifying public load increments `Views`, including repeated opens of the
same post by the same visitor within one session.

#### Scenario: Back to list then reopen counts twice

- GIVEN a public reader opens a post (`Views` goes `N → N + 1`)
- WHEN the reader returns to the list and opens the same post again
- THEN a second `GET /api/blogposts/{idOrSlug}` is issued
- AND the post's persisted `Views` becomes `N + 2`

## Non-Goals

- Per-session, per-day, or per-visitor view deduplication.
- Any change to Admin/Owner counting rules (already enforced server-side).
- Rendering the `Views` value on the public detail page.
- A dedicated increment endpoint decoupled from the data `GET` (kept as a
  follow-up; the current design relies on every in-app entry point routing its
  single `GET` through the detail page).
- Any backend, controller, endpoint, DTO, or database change.
- Deciding the `404`-specific background-GET outcome (design phase).
