# Tasks: Blog Post View Counter Increments on In-App Reads

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~185 authored (`BlogPostDetailPage.tsx` ~35, `BlogPostDetailPage.test.tsx` ~150) |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | n/a (single PR) |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: n/a (single PR)
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Fire the background GET on the router-state path so public reads increment `Views` | PR 1 | `npm run test --prefix Club12-WebClient` | Browser: anon "Leer más" → `/panel` Vistas +1; admin "Ver" → no change | Revert the single commit (both files); no schema/data/backend state |

Frontend-only change — no backend task group (per `openspec/config.yaml` split rule).

## Phase 1: RED — hook-boundary tests (`BlogPostDetailPage.test.tsx`)

Author these against the current source; each MUST be observed failing with the design's expected RED signal before Phase 3. Tasks 1.2–1.7 are independent and may be written in parallel; 1.1 comes first.

- [x] 1.1 Add the `deferred<T>()` promise helper to the test file; keep `vi.mock('@/modules/blogPost/hook/blogPost.hook')` and the `buildPost` / `renderAt` helpers.
- [x] 1.2 RED Test 1 (rewrite) `renders the post from router state and still fires exactly one background GET`: before resolve `getByText('Post desde el estado')` present AND `queryByRole('status')` null; `getBlogPostsById` called once with `('mi-slug', { silent: true })`. Spec: *Instant Render From Router-State Post*, *In-App Read Increments the View Counter*.
- [x] 1.3 RED Test 2 `replaces the state post with the fetched copy on resolve`: deferred resolves to `buildPost({ title: 'Post del servidor', views: 8 })`; after `act(resolve)` `findByText('Post del servidor')`, `queryByText('Post desde el estado')` null, `queryByRole('status')` null throughout. Spec: *Fetched Server Copy Replaces the Displayed Post*.
- [x] 1.4 RED Test 3 `keeps showing the state post and raises no alert when the background GET fails`: `getBlogPostsById` → `mockResolvedValue(undefined)`, `vi.mock('sweetalert2')` spy; `state.post` stays, `queryByText('Publicación no encontrada')` null, `Swal.fire` not called. Spec: *Silent Failure Keeps the Router-State Post*.
- [x] 1.5 RED Test 4a (keep + extend) `fetches the post by id when no router state is present`: add `toHaveBeenCalledTimes(1)`. Spec: *Direct-URL Read Increments the View Counter*.
- [x] 1.6 RED Test 4b (keep) `shows a not-found page when the fetched post does not exist` — unchanged cold 404 path. Spec: *Direct-URL Read* / not-found branch.
- [x] 1.7 RED Test 4c (new) `shows the skeleton while the cold fetch is in flight`: deferred; before resolve `getByRole('status')` present, after resolve absent. Spec: *Instant Render From Router-State Post* (skeleton discriminator).

## Phase 2: RED — network-boundary test (`BlogPostDetailPage.test.tsx`, own `describe`)

- [x] 2.1 RED Test 5 (new) `issues one network GET per mount, even across remounts` in its own `describe` with `vi.mock('@/modules/core/utils/axiosUtils')` on `sendGet`, real `QueryClientProvider` (one shared `new QueryClient()`), `ErrorProvider`, `BlogPostProvider`, wrapped in `<StrictMode>`: mount 1 → `sendGet` called 1×; `unmount()`; mount 2 → 2×. Spec: *Every Qualifying Load Counts*, *In-App Read Increments the View Counter*.

## Phase 3: GREEN — implementation (`BlogPostDetailPage.tsx:1,30-55`)

- [x] 3.1 Apply the design's after-sketch: import `useRef`; extract `seededPost`; `loading` starts `!seededPost`; add `routeKeyRef = useRef(seededPost ? idOrSlug : undefined)`; guard → `if (!idOrSlug) return;`; `setLoading(true)` only when `routeKeyRef.current !== requestedFor`; on resolve `setPost(fetched)` + `routeKeyRef.current = requestedFor`, else `setPost(undefined)` only when `routeKeyRef.current !== requestedFor`; `post` leaves deps → `[idOrSlug, getBlogPostsById]`. Run `npm run test --prefix Club12-WebClient` — Tests 1–5 GREEN.
  - **Batch 2 (post-QA)**: shipped implementation adds `requestedForRef` (fetch/count at most once per `idOrSlug`) and **removes** the `cancelled` cleanup flag — it left the cold-path skeleton stuck when paired with the guard's early return. Stale-response protection is now `if (requestedForRef.current !== requestedFor) return;` inside the async body. See `apply-progress.md` "Batch 2" and `design.md` "StrictMode double-invoke — QA UPDATE".

## Phase 4: REFACTOR — comments

- [x] 4.1 Replace the stale comment at `:45-46` (failed load now falls to not-found on the cold path only); add the `staleTime 0` coupling comment at the fetch site referencing Test 5. Re-run `npm run test --prefix Club12-WebClient` — still GREEN.

## Phase 5: Verification

- [x] 5.1 Lint + typecheck: `cd Club12-WebClient && npx eslint src/views/blogPost --max-warnings 0 && npx tsc --noEmit` — exit 0, no `exhaustive-deps` suppression. (both exit 0)
- [x] 5.2 Full suite: `dotnet test Club12-Backend/Solution/Club12.sln && npm run test --prefix Club12-WebClient` — all green; backend byte-identical. (frontend **503/503** after Batch 2, backend 728/728 in the apply run; commit `0d7eda9` touches no backend file)
- [x] 5.3 Manual: as anonymous user open a post via "Leer más" → confirm `/panel` `Vistas` +1 (exactly one). Repeat via admin "Ver" → confirm `Vistas` unchanged. Spec: *In-App Read Increments*, *Admin and Owner Reads Do Not Increment*. **NOW DONE — user verified in production** (Batch 3 complete; task re-checked per final-state authority).

## Key Learnings

1. Removing the `if (post) return;` guard and dropping `post` from the effect dependency array must happen in the same edit, or `staleTime 0` turns every fetched-object identity change into an unbounded GET-and-increment loop.
2. Each design test-table row (1, 2, 3, 4a, 4b, 4c, 5) is authored and observed RED as its own task before the single implementation task under strict TDD.
3. Test 5 mocks at the axios `sendGet` boundary under `React.StrictMode` because that is the only level where "one network GET per mount" — the thing `Views` actually counts — is a meaningful assertion.
4. The change is frontend-only and needs no backend task group; the backend counter in `BlogPostController.GetBlogPostById` is already correct and its tests stay untouched.
5. Admin "Ver" non-increment is preserved with zero client work because increment eligibility is decided server-side from the JWT role via `includeUnpublished`.
