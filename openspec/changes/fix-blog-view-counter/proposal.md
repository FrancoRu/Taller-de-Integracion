# Proposal: Blog Post View Counter Increments on In-App Reads

**Touches**: **frontend only** — one component plus its test file. No backend, API, endpoint, DB, schema, or client-service change.

## Intent

Reported (ES): "El contador de visitas de las noticias no aumenta cuando alguien entra y ve la noticia." An admin watching the `Vistas` column in `/panel` sees it stay flat while readers open posts.

The backend counter is correct and tested: `BlogPostController.GetBlogPostById` increments `Views` for every non-admin caller (`BlogPostViewCounterTests` green). The break is on the client. `BlogPostDetailPage.tsx:39-55` bails on `if (post || !idOrSlug) return;`, and both `showPosts.tsx:93-95` ("Leer más") and `BlogPostsPage.tsx:100-105` (admin "Ver") navigate with `{ state: { post } }`. So `GET /api/blogposts/{idOrSlug}` — the only `Views++` trigger — never fires in the normal in-app flow. Only a direct URL open or hard refresh by a non-admin counts.

## Scope

### In Scope

- `BlogPostDetailPage.tsx`: relax the guard to `if (!idOrSlug) return;` so the GET always fires.
- Keep instant paint from `location.state.post`: no skeleton, never `setLoading(true)` while a post is already displayed.
- Always fire `getBlogPostsById(idOrSlug, { silent: true })` in the background; on resolve, `setPost(fetched)` (silent re-render with fresh content).
- Rewrite `BlogPostDetailPage.test.tsx` — the "renders from router state without fetching" case now asserts the opposite — plus new cases: exactly one background GET on the state-present path, no skeleton flash, silent failure keeps showing `state.post` with no global alert.

### Out of Scope (Non-Goals)

- Any backend, controller, endpoint, DTO, or DB change.
- View-count semantics: "every load counts" stays; no per-session/day dedupe.
- Admin/Owner counting rules: unchanged. The backend `includeUnpublished` guard already blocks increments for admin "Ver", so the fix needs no extra work there.
- Rendering `Views` on the public detail page.

## Capabilities

### New Capabilities

- `blog-post-view-counter`: a public read of a blog post MUST increment the counter once per load through the in-app navigation path, not only on direct-URL entry; admin/owner reads MUST NOT increment.

### Modified Capabilities

- None.

## Approach

Exploration Approach 1. Minimal diff, unchanged direct-URL flow, preserved instant UX. `queryClient.fetchQuery` with `blogPostKeys.byId` already dedupes concurrent identical keys, so the effect stays simple. An optional `useRef` "already counted for this idOrSlug" guard (Approach 2) may be folded in during apply if cheap.

### Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Dedicated `POST /api/blogposts/{id}/views` endpoint | Backend change; scope is locked to frontend only |
| Pure fire-and-forget (ignore the response) | User wants the displayed post refreshed with server state on resolve |

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Club12-WebClient/src/views/blogPost/BlogPostDetailPage.tsx:34-55` | Modified | Guard relaxed; seed from state; background silent GET; `setPost(fetched)` |
| `Club12-WebClient/src/views/blogPost/BlogPostDetailPage.test.tsx` | Modified | First test rewritten; three new cases |
| `showPosts.tsx`, `BlogPostsPage.tsx`, `blogPost.context.tsx`, `blogPost.service.ts`, `blogPost.hook.ts` | Unchanged | Navigation shortcut and data layer stay as-is |
| Backend (`BlogPostController`, entity, DTO, tests) | Unchanged | Counter already correct |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| React StrictMode (`main.tsx:59`) double-invokes the effect in dev | Med (dev only) | `fetchQuery` dedupes concurrent same-key calls → 1 network call; prod invokes once; optional `useRef` counted-guard during apply |
| A future non-zero `staleTime` on the QueryClient or this query serves the GET from cache and silently re-breaks counting | Med | Code comment at the fetch site stating the coupling; follow-up item to make the dependency explicit |
| Background GET 404 (post deleted between list and open) | Low | `silent: true` suppresses the alert and keeps `state.post`; design phase decides whether to fall to the not-found branch |
| One extra GET + DB write per in-app open | Low | Intended: "every load counts" is the kept semantics |
| Stale `state.post.views` shown briefly | None user-visible | Detail page never renders `views`; fetched post replaces state silently |

## Rollback Plan

Revert the single `BlogPostDetailPage.tsx` commit (and its test file). The counter returns to direct-URL-only behavior — the pre-change state. No data migration, no schema change, no backend deploy, no stored state to unwind. Already-recorded `Views` values remain valid.

## Dependencies

- Existing `useBlogPost().getBlogPostsById` with `{ silent: true }` support.
- `QueryProvider.tsx:4` bare `new QueryClient()` (staleTime 0).
- Integration branch is `develop`. Strict TDD active. Delivery: `ask-on-risk`, review budget 1500 lines — single PR expected.

## Success Criteria

- [ ] Opening a post via "Leer más" as an anonymous/public user issues exactly one `GET /api/blogposts/{idOrSlug}` and `Vistas` increases by 1 in `/panel`.
- [ ] No skeleton flash: the post renders immediately from `location.state.post`.
- [ ] The displayed post is replaced by the fetched response on resolve.
- [ ] Direct URL / hard refresh behavior unchanged (still one increment).
- [ ] Admin "Ver" still does not increment `Views`.
- [ ] A failed background GET keeps the state post visible and raises no global alert.
- [ ] Frontend suite green (`npm run test --prefix Club12-WebClient`); backend suite untouched and green.

## Proposal question round

Scope was locked by the user before this phase and no user turn is available here. These are recorded as approved decisions, not open questions; flag only if the user disagrees:

1. View-count semantics stay "every load counts" — back-to-list-then-reopen legitimately counts twice.
2. The fetched post replaces the state post on resolve (chosen over pure fire-and-forget).
3. A new `blog-post-view-counter` capability spec is warranted to pin the intended behavior; no existing spec covers the blog-post read path.
4. Whether a 404 on the background GET should fall through to the not-found page is deferred to the design phase.
