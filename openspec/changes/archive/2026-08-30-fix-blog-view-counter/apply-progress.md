# Apply Progress: fix-blog-view-counter

**Batch**: 1 (first and only) · **Mode**: Strict TDD · **Branch**: `fix/blog-view-counter` (off `origin/develop`)
**Attempt token**: `sha256:ae654d6d16cc001a0ca8624deaa8b8f1627f329800860e52c59bdd1c09970272`
**Delivery**: single PR, `ask-on-risk`, review budget 1500 lines. `size:exception` not needed (~185 authored lines). No PR opened — stopped after implementation + verification per prompt.

## Task Status — cumulative

| Task | Status | Notes |
|------|--------|-------|
| 1.1 `deferred<T>()` helper + kept `buildPost`/`renderAt`/hook mock | [x] | Helper added; `renderAt` kept; added `withGetBlogPostsById` helper |
| 1.2 RED Test 1 (rewrite) exactly-one background GET on router-state path | [x] | RED: `expected "vi.fn()" to be called 1 times, but got 0 times` |
| 1.3 RED Test 2 fetched copy replaces state post | [x] | RED: `Unable to find an element with the text: Post del servidor` |
| 1.4 RED Test 3 silent failure keeps state post, no alert | [x] | RED: `expected "vi.fn()" to be called with [ 'mi-slug', { silent: true } ]` |
| 1.5 RED Test 4a (keep+extend) cold fetch + `toHaveBeenCalledTimes(1)` | [x] | Regression guard — GREEN before and after (expected) |
| 1.6 RED Test 4b (keep) cold 404 not-found page | [x] | Regression guard — GREEN before and after (expected) |
| 1.7 RED Test 4c (new) skeleton visible during cold fetch | [x] | Regression guard — GREEN before and after (skeleton discriminator for Test 1) |
| 2.1 RED Test 5 one network GET per mount across remounts | [x] | RED: `expected "vi.fn()" (sendGet) to be called 1 times, but got 0 times` |
| 3.1 GREEN implementation (design after-sketch) | [x] | All 7 tests GREEN |
| 4.1 REFACTOR comments (stale `:45-46` replaced, staleTime coupling comment) | [x] | Folded into the 3.1 edit; re-run GREEN |
| 5.1 Lint + `tsc --noEmit` | [x] | Both exit 0, no `exhaustive-deps` suppression |
| 5.2 Full frontend + backend suites | [x] | Frontend 501/501 (106 files); backend 728/728; backend absent from `git diff` |
| 5.3 Manual browser view-count check | [ ] | PENDING — not executable here. Proxies green: Test 1 + Test 5 + backend `BlogPostViewCounterTests` |

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.2 Test 1 | `BlogPostDetailPage.test.tsx` | Component (RTL) | ✅ 3/3 pre-existing | ✅ 0 calls vs expect 1 | ✅ Passed | ➖ paired with Test 5 at network layer | ✅ comments only |
| 1.3 Test 2 | same | Component (RTL) | ✅ | ✅ text not found | ✅ Passed | ➖ single scenario | ✅ |
| 1.4 Test 3 | same | Component (RTL) | ✅ | ✅ call-args assertion fails | ✅ Passed | ✅ pairs with Test 4b (cold path still 404s) | ✅ |
| 1.5 Test 4a | same | Component (RTL) | ✅ | ➖ guard (stays GREEN) | ✅ Passed | ✅ vs Test 1 (state-present path) | ➖ |
| 1.6 Test 4b | same | Component (RTL) | ✅ | ➖ guard (stays GREEN) | ✅ Passed | ✅ vs Test 3 (warm path keeps content) | ➖ |
| 1.7 Test 4c | same | Component (RTL) | ✅ | ➖ guard (stays GREEN) | ✅ Passed | ✅ vs Test 1 (no-skeleton discriminator) | ➖ |
| 2.1 Test 5 | same, own `describe` | Integration (real QueryClient + providers + StrictMode, axios `sendGet` mocked) | ✅ | ✅ 0 network calls vs expect 1 | ✅ Passed (1 then 2) | ✅ across-remount count is the triangulation | ➖ |
| 3.1 impl | `BlogPostDetailPage.tsx` | — | ✅ full suite before | — | ✅ 7/7 | — | ✅ comments |

### Test Summary
- Tests written/changed: 7 (1 rewritten, 3 new, 1 extended, 2 kept) across 2 `describe` blocks
- Tests passing: 7/7 file · 501/501 frontend suite · 728/728 backend
- Layers: Component/RTL (6), Integration (1)
- Approval tests: none (behaviour change, not a pure refactor)
- Pure functions created: 0 (React component with effect state; `deferred<T>()` test helper is pure)

## Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command + result | `npx vitest run src/views/blogPost/BlogPostDetailPage.test.tsx` → 7 passed (was 4 failed / 3 passed at RED) |
| Runtime harness command/scenario + result | Test 5 is the runtime path: real `QueryClient` + `ErrorProvider` + `BlogPostProvider` under `<StrictMode>`, `sendGet` mocked at the axios boundary; mount → `sendGet` 1×, `unmount()`, remount → 2×. GREEN. Full browser stack check (5.3) still pending — no running stack here. |
| Rollback boundary | Revert the single commit touching `BlogPostDetailPage.tsx` + `BlogPostDetailPage.test.tsx`. No schema/data/backend/config state. `openspec/config.yaml` change was pre-existing on the working tree, unrelated. |

## Deviations from design

- **`<React.StrictMode>` → `import { StrictMode } from 'react'`**: the `react-19` skill bans `import React`, and the ESLint flat config enforces it. Named import is behaviourally identical. tasks.md 2.1 wording updated to `<StrictMode>`.
- **Test 5 real-hook wiring**: the file-level `vi.mock('@/modules/blogPost/hook/blogPost.hook')` would auto-mock `useBlogPost` for the whole file, breaking the real-provider path. Test 5's `describe` restores the real hook via `vi.importActual` → `mockedUseBlogPost.mockImplementation(actual.useBlogPost)`; the real `BlogPostContext`/`BlogPostProvider` are untouched by any mock. This is a test-mechanics choice within the design's stated "own describe with different mock surface", not a design change.
- **Contingency (Decision 2 `countedForRef`) NOT applied**: Test 5 went GREEN with count 1 on the StrictMode mount and 2 across the remount. `queryClient.fetchQuery` same-key dedupe held exactly as the design predicted, so the 5-line counted-ref guard was not folded in.
- Phase 4 comments were written as part of the Phase 3 edit rather than a separate pass (same work unit); re-run confirmed still GREEN.

## Issues found

None. Backend untouched and green. No `eslint-disable` anywhere; `react-hooks/exhaustive-deps` satisfied by `[idOrSlug, getBlogPostsById]` (design's dependency-stability analysis holds).

## Remaining

- 5.3 manual browser verification (needs a running stack).
- Open the single PR (explicitly deferred by the apply prompt).

---

## Batch 2 — post-QA fix: StrictMode double-count

**Trigger**: user manual-tested `npm run dev` and reported the counter moving **"de a 2"** per visit. DevTools Network showed **2** `GET /api/blogposts/{slug}` per single visit.

**Root cause** (systematic-debugging, confirmed by user evidence): `main.tsx:59` wraps the app in `<React.StrictMode>`, so in dev every mount runs the fetch effect twice. In the real browser the gap between StrictMode's cleanup and re-setup is long enough for the first (localhost) GET to resolve; by the second setup the react-query entry is no longer in-flight and — with `staleTime` 0 — `fetchQuery` fires a **second** network GET, so the backend runs `Views++` twice. The `cancelled` flag only discarded the *displayed* result, never the request or the server-side increment. jsdom collapses the StrictMode timing (synchronous double-invoke), so react-query's in-flight dedupe held there and Test 5 never caught it — a test/reality gap. The design's Decision 2 bet on that dedupe; the bet was wrong in the browser.

**Fix** (`BlogPostDetailPage.tsx`, same 2 files, still frontend-only):
- Added `requestedForRef` — the effect fetches (and therefore counts) **at most once per `idOrSlug`** for the component's lifetime. StrictMode's second setup hits `requestedForRef.current === idOrSlug` and returns before any second `getBlogPostsById` call.
- Removed the per-effect `cancelled` flag (it fought the ref guard: with an early-return second setup, a `cancelled` first load left the cold-path skeleton stuck forever). Stale-response protection is now `requestedForRef.current !== requestedFor` inside `loadPost`, which still discards a response superseded by a later navigation.
- A real navigation unmounts the component → fresh `requestedForRef` → re-opening a post still counts (spec req 7 preserved).

**Tests** (`BlogPostDetailPage.test.tsx`):
- NEW `fires the background GET only once under a StrictMode double mount` — RED against Batch 1 code: `expected vi.fn() to be called 1 times, but got 2 times`. GREEN after the ref guard.
- NEW `fires the background GET again after a real remount for the same slug` — guards that the ref does not suppress legitimate re-visits (count 2 across unmount/remount).
- Test 5 (`network boundary`) still GREEN (1 per mount, 2 across remount).

**Verification**: `BlogPostDetailPage.test.tsx` 9/9 · frontend suite **503/503** (106 files) · `eslint --max-warnings 0` + `tsc --noEmit` clean · backend untouched. 5.3 manual re-check (Network tab now shows **1** GET per visit in dev) still pending a running stack on the user side.

**Design doc**: Decision 2 was reversed — the `countedForRef`-style guard is now implemented (in a form that also fixes the cold-path skeleton). `design.md` updated.

Batch 2 shipped as PR #91 (merged to `develop`, commit `5f4be40`).

---

## Batch 3 — post-QA fix: home page pre-fetch double-count

**Trigger**: after PR #91 was in production the user still saw the counter move **"de a 2"** — but only when opening a post **from the home page** ("Últimas noticias"), two GETs sequential with a gap.

**Root cause** (systematic-debugging, `grep getBlogPostsById` across the whole frontend): `home.tsx` `handleReadMore` **pre-fetched** the post — `await getBlogPostsById(idOrSlug)` fires `GET /api/blogposts/{slug}` (the only `Views++` trigger) — and *then* navigated with the result in router state. The detail page (Batch 1/2) then fires the same GET again. Two increments per home-originated visit. The Novedades path (`showPosts.tsx`) never had this because it navigates with the in-memory list post and does not pre-fetch. The exploration mapped `showPosts.tsx` but not `home.tsx`'s news section; a frontend-wide grep for `getBlogPostsById` at exploration time would have caught it.

**Fix** (`home.tsx` only, still frontend-only):
- `handleReadMore(post)` now navigates with the post it already holds from the `getBlogPostsByFilters` list fetch (`{ state: { post } }`), matching `showPosts.tsx`. No pre-fetch.
- `getBlogPostsById` removed from the `useBlogPost()` destructure (now unused in `home.tsx`).
- The detail page is the single owner of `GET /api/blogposts/{slug}` for every public entry point (Novedades, home, direct URL).

**Test** (NEW `src/views/home/home.test.tsx`):
- `navigates with the already-loaded post and does not pre-fetch it` — RED against pre-fix code (`getBlogPostsById` called 1×), GREEN after. Asserts `navigate('/blog/{slug}', { state: { post } })` and `getBlogPostsById` **not** called.

**Verification**: `home.test.tsx` + `src/views/blogPost/` 10/10 · frontend suite **504/504** (107 files) · `eslint src/views/home --max-warnings 0` + `tsc --noEmit` clean · backend untouched.

**Entry-point matrix after Batch 3** — every public visit counts exactly once:
| Entry point | Pre-fetch? | Detail GET | Increment |
|---|---|---|---|
| Novedades → "Leer más" | no | 1 | +1 |
| Home → "Últimas noticias" | no (was yes) | 1 | +1 |
| Direct URL / refresh | n/a | 1 | +1 |
| Admin "Ver" | no | 1 | +0 (server skips on JWT role) |
