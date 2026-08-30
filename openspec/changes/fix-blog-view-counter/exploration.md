# Exploration: fix-blog-view-counter

## Problem

Reported (ES): "El contador de visitas de las noticias no aumenta cuando alguien
entra y ve la noticia." An admin watching the "Vistas" column in `/panel` sees it
stay flat while readers open posts through the site.

## Current State (verified against source)

### Backend counter works
- `Club12-Backend/API/Controllers/BlogPostController.cs:137-161` —
  `GetBlogPostById(string idOrSlug)`, `[AllowAnonymous] [HttpGet("{idOrSlug}")]`.
  - Line 141: `includeUnpublished = User.IsInRole(Admin) || User.IsInRole(Owner)`.
  - Lines 153-157: `if (!includeUnpublished) { blogPost.Views++; await blogPostService.UpdateBlogPostAsync(blogPost); }`.
- `Club12-Backend/API.Tests/BlogPostViewCounterTests.cs` — public view 7→8 with
  `UpdateCount == 1`; admin view stays 7 with `UpdateCount == 0`. Passing.
- `Club12-Backend/Domain/Entities/Models/BlogPost.cs:15` — `public int Views { get; set; } = 0;`.
- `Club12-Backend/Application/DTOs/BlogPosts/Response/BlogPostResponse.cs:30` —
  `public int Views { get; set; }`. Mapped by name convention in
  `API/AutoMapperProfiles/BlogPostProfile.cs:22` (no `Ignore` on `Views`).
- `GenericRepository.UpdateAsync` — `_dbSet.Update(entity); SaveChangesAsync()`. Persists.
- `Club12-WebClient/src/modules/blogPost/type/blogPost.d.ts:181` — `views: number;`.

### Root cause — FRONTEND
The only call that issues `GET /api/blogposts/{idOrSlug}` (the increment trigger)
is `useBlogPost().getBlogPostsById` → `blogPostService.getBlogPostsById`
(`blogPost.service.ts:95-98`, `sendGet`). In the normal in-app flow it never runs:

- `Club12-WebClient/src/views/blogPost/showPosts.tsx:93-95` — `handleReadMore`:
  `navigate(APP_ROUTES.blogPost.build(post.slug), { state: { post } })`. The list
  response already carries full `markdownText`, so it deliberately reuses the
  in-memory post to avoid a "redundant" fetch.
- `Club12-WebClient/src/views/blogPost/BlogPostDetailPage.tsx:34-36` — `post`
  state seeded from `(location.state as BlogPostLocationState)?.post`; line 37
  `loading` starts `false` when a post is present.
- `BlogPostDetailPage.tsx:39-55` — `useEffect` bails at
  `if (post || !idOrSlug) return;`, so `getBlogPostsById` is skipped whenever
  router state carried a post.
- `Club12-WebClient/src/views/blogPost/BlogPostsPage.tsx:100-105` — admin
  `handleView` also navigates with `{ state: { post: row } }` (same shortcut).

Net: the counter only moves on a direct URL open / hard refresh by a non-admin
(no `location.state`). "Leer más" and admin "Ver" never increment.

### Where the count is observed
The public detail page renders no `Views`. The only UI showing it is the admin
list DataGrid column `Club12-WebClient/src/views/blogPost/BlogPostsPage.tsx:159` —
`{ field: 'views', headerName: 'Vistas', ... }`. That list loads via
`getBlogPostsByFilters` (`GET /api/blogposts`), which never increments.

### fetchQuery / caching
`blogPost.context.tsx:127-144` — `getBlogPostsById` returns `response?.data` via
`queryClient.fetchQuery({ queryKey: blogPostKeys.byId(idOrSlug), ... })`.
`QueryProvider.tsx:4` uses bare `new QueryClient()` (staleTime 0), so `fetchQuery`
always hits the network → increment fires on every load. Works only while
staleTime stays 0.

## Decisions (fixed for this change)

- **Fix approach: frontend only.** `BlogPostDetailPage` must always call
  `getBlogPostsById(idOrSlug, { silent: true })` even when `location.state.post`
  exists: render instantly from state (no skeleton flash), fire the GET in the
  background, swap in the fetched post on resolve. No backend / endpoint / client
  service changes.
- **View-count semantics: out of scope.** Keep current "every load counts"
  behavior; no per-session/day dedupe.
- **Admin/Owner views must stay uncounted** (backend already guards this).

## Affected Areas (frontend-only)

| File | Change |
|------|--------|
| `src/views/blogPost/BlogPostDetailPage.tsx` | Core: relax guard to `if (!idOrSlug) return;`; seed from `state.post`; never `setLoading(true)` when a post is already shown; `setPost(fetched)` on resolve; keep `{ silent: true }`. |
| `src/views/blogPost/BlogPostDetailPage.test.tsx` | First test ("renders from router state without fetching", asserts `getBlogPostsById` not called) BREAKS — rewrite: renders from state immediately AND fires exactly one background GET. Other two tests stay. |
| `showPosts.tsx`, `BlogPostsPage.tsx`, `blogPost.context.tsx`, `blogPost.service.ts`, `blogPost.hook.ts` | No change. |

New test coverage to add:
- state-present path fires exactly one GET;
- no skeleton flash when `state.post` present;
- silent background failure keeps showing `state.post` (no global alert).

## Approaches considered

| # | Approach | Effort | Notes |
|---|----------|--------|-------|
| 1 | Drop `post` guard, keep `!idOrSlug` guard; seed from `state.post`, never `setLoading(true)` when a post is shown, `setPost(fetched)` on resolve | Low | Minimal diff, instant UX preserved, direct-URL flow unchanged. StrictMode double-invokes in dev. **Recommended.** |
| 2 | Approach 1 + `useRef` "already counted for this idOrSlug" guard | Low-Med | Hardens against double increment; `fetchQuery` already dedupes concurrent same-key calls, so mostly redundant in prod. Fold in during apply if cheap. |
| 3 | Dedicated lightweight increment endpoint | Med | Rejected — decision is no backend changes. |

## Risks / Edge Cases

- **React StrictMode** (`main.tsx:59`): dev double-invokes the effect;
  `queryClient.fetchQuery` dedupes concurrent identical `byId` keys → normally 1
  network call; prod invokes once. Low risk; optional ref guard removes it.
- **Stale `state.post.views`**: not user-visible (detail page never renders
  views); fetched post silently replaces state.
- **Back-to-list then re-open**: 2 GETs = 2 increments — consistent with the kept
  "every load counts" semantics.
- **Direct URL / refresh**: still no `location.state` → already fetches → unchanged.
- **Background GET 404** (post deleted between list and open): with
  `silent: true` the alert is suppressed and the page keeps showing `state.post`.
  Decide in design whether to also fall to the not-found branch.
- **staleTime coupling**: a future non-zero `staleTime` / `Infinity` on the
  QueryClient or this query would serve the background GET from cache and stop
  incrementing. Note as a follow-up guard.

## Ready for Proposal

Yes — frontend-only, one core file plus one test rewrite.
