# Design: Blog Post View Counter Increments on In-App Reads

## Technical Approach

The backend is already correct (`BlogPostController.GetBlogPostById` increments
`Views` for every non-admin caller). The break is that `BlogPostDetailPage`
never issues the GET when the router carried a post. The whole fix is therefore
**one presentation-layer component plus its test file**.

The component moves from *"fetch only when I have nothing"* to
**"always fetch, render optimistically from whatever I already have"**:

- Seed `post` from `location.state.post` (unchanged).
- `loading` starts `!post` (unchanged) — the skeleton only exists for the
  cold, direct-URL path.
- The effect guard drops the `post` term: `if (!idOrSlug) return;`.
- The GET fires unconditionally in the background with `{ silent: true }`;
  on resolve `setPost(fetched)` swaps in the server copy.
- The skeleton is raised only when nothing belonging to *this route* is on
  screen, tracked by a `routeKeyRef` (see Decision 1).

### Clean Architecture

**N/A for the backend** — this change touches no `Domain`, `Application`,
`Infrastructure` or `API` code. On the client the equivalent boundary is
`modules/` (types, service, context, query keys) versus `views/`
(presentation). The change is confined to `views/blogPost/`; `blogPost.service.ts`,
`blogPost.context.tsx`, `blogPost.hook.ts`, `queryKeys.ts` and `QueryProvider.tsx`
are all untouched, so the data layer stays the single owner of transport and
caching.

## The `post`-in-deps trap (load-bearing)

Today the effect deps are `[idOrSlug, post, getBlogPostsById]` and the body
starts with `if (post) return;`. Those two facts are coupled: the guard is what
makes `post` a *safe* dependency.

Removing the guard **without** removing `post` from the deps array produces an
unbounded loop:

```
effect → fetchQuery (staleTime 0 → network) → setPost(newObject)
       → post identity changed → effect re-runs → fetchQuery → …
```

`getBlogPostsById` returns `response?.data`, a freshly deserialized object on
every network round-trip, so referential equality never stabilises. Each
iteration is one `GET` and one `Views++` on the server. **`post` must leave the
dependency array.** Every state that the effect needs to consult about the
currently displayed post is therefore read through a ref.

`react-hooks/exhaustive-deps` stays satisfied with `[idOrSlug, getBlogPostsById]`:
refs and `useState` setters are exempt. No lint suppression is required.

### Dependency stability — verified, not assumed

Dropping the `post` guard means the effect's *other* deps become the only thing
standing between us and repeated increments. Both were traced to their source:

| Dep | Stable? | Evidence |
|---|---|---|
| `idOrSlug` | Yes | `useParams` string, changes only on navigation |
| `getBlogPostsById` | **Yes** | `blogPost.context.tsx:127-144` — `useCallback(..., [queryClient, handleUnknownError])`. `queryClient` comes from `useQueryClient()` (module-level singleton in `QueryProvider.tsx:4`). `handleUnknownError` is `useUnknownErrorHandler()` → `useCallback(..., [setError])`; `setError` is `useCallback(..., [setMessage])` and `setMessage` is `useCallback(..., [])` in `error.context.tsx:39-76`. |

This matters because `ErrorProvider`'s context `value` memo *does* depend on the
`errors` array (`error.context.tsx:87-90`), so the provider re-renders whenever
an error is raised or auto-cleared 5 s later. Those re-renders propagate through
`BlogPostProvider` — and `BlogPostProvider`'s own `container` memo is *not*
stable, because `addBlogPost`/`putBlogPostById`/`deleteBlogPostById` close over
`useMutation` objects that change identity every render. Only `getBlogPostsById`
survives with a stable identity, and it is the one this component consumes. Had
`setError` been an inline arrow, this change would have turned every global
error toast into an extra view increment.

## Architecture Decisions

| # | Decision | Alternatives rejected | Rationale |
|---|----------|-----------------------|-----------|
| 1 | Track the route the displayed post belongs to in a `useRef<string>` (`routeKeyRef`), not in state | Read `post` inside the effect; compare `post.id === idOrSlug \|\| post.slug === idOrSlug` in render | A ref is the only way to consult "what is on screen" without putting `post` in the deps array (see the trap above). Seeding it is trivially correct: `location.state.post` is always set by `navigate(build(post.slug), { state: { post } })` in the same call, so at mount the seeded post *is* the post for `idOrSlug`. |
| 2 | ~~Rely on `queryClient.fetchQuery` same-key dedupe for StrictMode; do not add a counted-ref guard~~ **REVERSED after QA** — added a `requestedForRef` guard so the effect fetches at most once per `idOrSlug`, and removed the `cancelled` flag | Original: trust the dedupe | The dedupe is structural **only in jsdom**, where StrictMode's double-invoke is synchronous. In the browser the gap between cleanup and re-setup lets the first localhost GET resolve, so with `staleTime` 0 the second setup fires a real second GET and a second `Views++` (user saw the counter move "de a 2" in dev). The `requestedForRef` guard is the exploration's Approach 2, in a form that also fixes a cold-path skeleton-stuck bug the raw counted-ref would have caused. See "StrictMode" below. |
| 3 | Cancellation flag in the effect cleanup (`let cancelled = false`), not an `AbortController` | Abort the axios request; ignore the race | `getBlogPostsById` exposes no signal parameter and `sendGet` (`axiosUtils.ts:274-279`) accepts no config. A cancellation flag is the only mechanism available at this layer, and it is sufficient: the goal is to prevent a **stale `setPost`**, not to save the byte. Aborting would also cancel a request the server has already counted, making the client and the counter disagree. |
| 4 | A failed/empty background GET keeps `state.post` on screen; it falls to the 404 branch **only** when nothing for this route was displayed | Always fall through to not-found on `undefined` | `getBlogPostsById` swallows the error and returns `undefined` for *every* failure class — 404, 500, timeout, offline (`blogPost.context.tsx:139-141`, `catch { if (!options?.silent) handleUnknownError(error); }`). The component cannot tell "deleted" from "wifi dropped". Replacing a fully readable article with a 404 page on an ambiguous signal is strictly worse than showing content that is at most seconds stale. The cold path is unchanged: no post displayed + `undefined` → 404, exactly as today. |
| 5 | Comment the `staleTime` coupling at the fetch site; **assert it behaviourally**, never by reading `QueryProvider` config | `expect(queryClient.getDefaultOptions().queries.staleTime).toBe(0)` | Asserting the config tests a constructor argument, not a behaviour, and would fail for a `staleTime` set per-query instead. The mount→unmount→mount regression test (Test 5) fails for *any* cause of a cached, un-issued GET. `QueryProvider.tsx` is not modified. |
| 6 | Keep `loading` as a single boolean; raise it only when `routeKeyRef.current !== idOrSlug` | Separate `refreshing` state; `isPending` from `useQuery` | The background refresh is deliberately invisible — there is no UI to drive from a second flag. Migrating to `useQuery` would be a data-layer rewrite, out of scope. |

## StrictMode double-invoke — QA UPDATE

> **The section below was the original reasoning and it was wrong for the browser.**
> Manual testing (`npm run dev`) showed the counter incrementing by 2 and DevTools
> Network showed 2 `GET /api/blogposts/{slug}` per single visit. jsdom runs the
> StrictMode double effect-invoke synchronously, so `fetchQuery`'s in-flight
> dedupe held in tests; the real browser has a gap long enough for the first
> localhost GET to resolve, after which `staleTime` 0 makes the second setup fire
> a second GET and a second `Views++`. Fix shipped: a `requestedForRef` guard in
> the effect (`if (requestedForRef.current === idOrSlug) return;`), fetch at most
> once per `idOrSlug` per mount; the `cancelled` flag was removed and stale-
> response protection moved to a `requestedForRef.current !== requestedFor` check.
> A real navigation unmounts the component and resets the ref, so re-opening a
> post still counts (spec req 7). New RED test:
> `fires the background GET only once under a StrictMode double mount`.

## StrictMode double-invoke — original reasoning (superseded)

`main.tsx:59` wraps the app in `React.StrictMode`, so in dev the effect runs
mount → cleanup → mount within the same commit.

The dedupe is **structural, not incidental**:

1. `queryClient.fetchQuery({ queryKey, queryFn })` is evaluated *synchronously*
   before the `await` in `getBlogPostsById` suspends, and it registers the
   in-flight promise on the query for `blogPostKeys.byId(idOrSlug)` at that
   moment.
2. StrictMode's second invocation happens in the same synchronous commit, while
   that promise is still pending.
3. `fetchQuery` returns the existing in-flight promise for an identical key
   instead of starting a second one.

→ **one network request, one `Views++`,** even against a real backend in dev.
The "dev double increments pollute real data" premise does not materialise.

Adding a counted-ref would:

- make the "exactly one GET" assertion pass regardless of whether the dedupe
  actually holds, i.e. destroy the only signal we have;
- introduce a second, competing source of truth about "should I fetch" beside
  `routeKeyRef`, which the 404 branch also reads;
- buy nothing in production, where the effect runs once.

**Instead of a guard, add a test.** Test 5 mounts the page twice against one
shared `QueryClient` with `sendGet` mocked and asserts the network call count.
That converts the dedupe/staleTime assumption into an assertion.

**Contingency (recorded, not implemented):** if Test 5 ever goes RED because two
requests leave the client on a single mount, *then* add
`const countedForRef = useRef<string>()` with an early return when
`countedForRef.current === idOrSlug`, set immediately before the `fetchQuery`
call. It is a five-line change and this design does not preclude it — it just
refuses to ship it as speculative armour ahead of evidence.

The cancellation flag of Decision 3 is a different mechanism and does **not**
suppress the second fetch: it only discards a stale `setPost`.

## Sequence — in-app "Leer más" (the fixed path)

```
ShowPosts        Router          BlogPostDetailPage      context.getBlogPostsById   queryClient      API                DB
    │               │                     │                        │                    │            │                  │
 handleReadMore(post)                     │                        │                    │            │                  │
    ├─ navigate('/blog/mi-slug', { state: { post } }) ─▶            │                    │            │                  │
    │               ├─ mount ────────────▶│                        │                    │            │                  │
    │               │        useState post = state.post  ✔ full markdownText            │            │                  │
    │               │        useState loading = false     ← no skeleton                 │            │                  │
    │               │        routeKeyRef  = 'mi-slug'                                   │            │                  │
    │               │                     ├─ FIRST PAINT: article visible ──────────────────────────────────────────────▶ (user reads)
    │               │                     │                        │                    │            │                  │
    │               │        useEffect: idOrSlug present → no return; routeKeyRef === idOrSlug → NO setLoading(true)     │
    │               │                     ├─ getBlogPostsById('mi-slug', { silent:true })▶                               │
    │               │                     │                        ├─ fetchQuery(['blogPost','byId','mi-slug']) ─▶       │
    │               │                     │                        │   staleTime 0 ⇒ network                             │
    │               │                     │                        │                    ├─ GET /api/blogposts/mi-slug ──▶│
    │               │                     │                        │                    │   [AllowAnonymous]             │
    │               │                     │                        │                    │   includeUnpublished = false   │
    │               │                     │                        │                    │   Views++ ; UpdateBlogPostAsync┼─▶ UPDATE
    │               │                     │                        │◀── 200 BlogPostResponse (Views already incremented) │
    │               │                     │◀── BlogPostResponse ───┤                    │            │                  │
    │               │        cancelled === false → setPost(fetched); routeKeyRef = 'mi-slug'; setLoading(false)          │
    │               │                     ├─ SILENT RE-RENDER (same title/body, server-fresh) ────────────────────────────▶
```

Admin "Ver" (`BlogPostsPage.handleView`) follows the identical client chain; the
only difference is server-side — `User.IsInRole(Admin\|Owner)` makes
`includeUnpublished` true, so the `Views++` block is skipped. No client work is
needed to preserve that.

## Sequence — direct URL / hard refresh (unchanged) and background failure

```
Browser        BlogPostDetailPage          getBlogPostsById         API
   │                    │                        │                   │
 GET /blog/mi-slug ────▶│  location.state = null │                   │
   │                    │  post = undefined ; loading = TRUE          │
   │                    │  routeKeyRef = undefined                    │
   │                    ├─ DetailSkeleton (role="status") ──────────────▶
   │                    ├─ effect: routeKeyRef (undefined) !== 'mi-slug' → setLoading(true)  [no-op, already true]
   │                    ├─ getBlogPostsById ─────▶│                   │
   │                    │                         ├─ GET ────────────▶│  Views++
   │                    │◀── post ────────────────┤◀──────────────────┤
   │                    │  setPost ; routeKeyRef = 'mi-slug' ; setLoading(false)
   │                    ├─ article ─────────────────────────────────────▶

FAILURE BRANCH (404 / 500 / offline — indistinguishable, all yield `undefined`):

   state.post present         → routeKeyRef === idOrSlug → KEEP showing state.post,
                                setLoading(false), no setPost, NO global alert (silent)
   state.post absent (cold)   → routeKeyRef !== idOrSlug → setPost(undefined)
                                → 404 "Publicación no encontrada"   [today's behaviour]
```

## Code sketch — before / after

**Before** (`BlogPostDetailPage.tsx:30-55`):

```tsx
const { idOrSlug } = useParams<{ idOrSlug: string }>();
const location = useLocation();
const { getBlogPostsById } = useBlogPost();
const [post, setPost] = useState<BlogPostResponse | undefined>(
  (location.state as BlogPostLocationState | undefined)?.post
);
const [loading, setLoading] = useState(!post);

useEffect(() => {
  if (post || !idOrSlug) return;                 // ← blocks the only Views++ trigger

  const loadPost = async () => {
    setLoading(true);
    try {
      // Suppress the global blocking alert on the initial GET; a failed
      // load falls through to the quiet inline "not found" state below.
      const fetchedPost = await getBlogPostsById(idOrSlug, { silent: true });
      setPost(fetchedPost ?? undefined);
    } finally {
      setLoading(false);
    }
  };

  loadPost();
}, [idOrSlug, post, getBlogPostsById]);          // ← `post` is only safe while the guard exists
```

**After**:

```tsx
const { idOrSlug } = useParams<{ idOrSlug: string }>();
const location = useLocation();
const { getBlogPostsById } = useBlogPost();
const seededPost = (location.state as BlogPostLocationState | undefined)?.post;
const [post, setPost] = useState<BlogPostResponse | undefined>(seededPost);
const [loading, setLoading] = useState(!seededPost);

/**
 * The `idOrSlug` the currently displayed post belongs to, or undefined when
 * nothing is displayed. Deliberately a ref, not state: the fetch effect has to
 * consult it, and taking `post` as a dependency would re-run the effect on every
 * setPost — with staleTime 0 that is an unbounded fetch/increment loop.
 * Seeding it with `idOrSlug` is sound because the navigation that supplies
 * `location.state.post` builds the URL from that same post.
 */
const routeKeyRef = useRef<string | undefined>(seededPost ? idOrSlug : undefined);

useEffect(() => {
  if (!idOrSlug) return;

  // The GET is unconditional on purpose: GET /api/blogposts/{idOrSlug} is the
  // *only* thing that increments Views on the server, so skipping it when the
  // post arrived via router state is what made the "Vistas" column stay flat.
  //
  // COUPLING: this relies on the QueryClient keeping staleTime 0
  // (QueryProvider.tsx) so fetchQuery always reaches the network. A non-zero
  // staleTime — global or per-query — would serve this from cache and silently
  // stop the counter. BlogPostDetailPage.test.tsx locks it behaviourally with a
  // mount → unmount → mount test that expects two network GETs.
  let cancelled = false;

  // Only blank the page when nothing for THIS route is on screen. When the post
  // came in via location.state the refresh runs invisibly underneath it.
  if (routeKeyRef.current !== idOrSlug) setLoading(true);

  const loadPost = async () => {
    try {
      // silent: a failed refresh must not raise the global blocking alert over
      // an article the reader is already reading.
      const fetchedPost = await getBlogPostsById(idOrSlug, { silent: true });
      if (cancelled) return;

      if (fetchedPost) {
        setPost(fetchedPost);
        routeKeyRef.current = idOrSlug;
      } else if (routeKeyRef.current !== idOrSlug) {
        // Cold path only. `undefined` conflates 404 / 500 / offline, so it may
        // not tear down an article that is already readable.
        setPost(undefined);
      }
    } finally {
      if (!cancelled) setLoading(false);
    }
  };

  void loadPost();

  return () => {
    cancelled = true;
  };
}, [idOrSlug, getBlogPostsById]);
```

Everything from `usePageMetadata` (`:60-65`) downward is unchanged. The metadata
hook re-runs with the fetched post and updates the OG tags to the server copy —
a free improvement, not a behaviour this change needs.

The stale comment at `:45-46` must be replaced, not kept: after this change a
failed load only falls through to "not found" on the cold path.

### Idempotency / race (`idOrSlug` changes mid-flight)

React Router reuses the same component instance when only the param changes, so
this is a dep change, not a remount. The ordering is:

1. `idOrSlug` `'a'` → `'b'`.
2. Cleanup of the `'a'` effect runs first: `cancelled = true`.
3. The `'b'` effect runs: `routeKeyRef.current === 'a' !== 'b'` → `setLoading(true)`
   → skeleton, and fires the `'b'` GET.
4. The `'a'` response lands late and is discarded at `if (cancelled) return;` —
   `routeKeyRef` is never written by a stale response, so it cannot corrupt the
   404 decision for `'b'`.

The `'a'` GET is *not* aborted, and that is correct: the server already counted
it, and the user really did open post `'a'`.

**Latent bug fixed as a side effect:** today the `if (post) return;` guard means
an in-place `idOrSlug` change keeps rendering the *previous* post under the new
URL forever. After this change the new post is fetched and rendered.

**Known limitation (unchanged, documented):** on such an in-place navigation the
`useState` initializer does not re-run, so a `location.state.post` supplied by a
second `navigate` is ignored and the skeleton shows briefly. No route in the app
navigates detail→detail today; if one is added, promote the seed to a
`location.key`-keyed effect or add `key={idOrSlug}` at the route element.

## Testing Strategy (Strict TDD — RED first)

Tests are written and observed failing **before** `BlogPostDetailPage.tsx` is
touched. Expected RED signals per test are listed below — a test that passes
against the current implementation is not a valid RED.

`vi.mock('@/modules/blogPost/hook/blogPost.hook')` and the existing `buildPost` /
`renderAt` helpers are kept. Tests 1-4 mock at the hook boundary; Test 5 mocks at
the axios boundary because it is the only level where "one network GET" is a
meaningful claim.

Add a deferred-promise helper so the in-flight window is inspectable rather than
racing `waitFor`:

```tsx
const deferred = <T,>() => {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>(r => { resolve = r; });
  return { promise, resolve };
};
```

| # | Test | Mocks | Key assertions | RED because |
|---|------|-------|----------------|-------------|
| 1 | **rewrite** `renders the post from router state and still fires exactly one background GET` | hook; `getBlogPostsById` → deferred | Before resolve: `getByText('Post desde el estado')` present **and** `queryByRole('status')` is `null` (no skeleton flash). `expect(getBlogPostsById).toHaveBeenCalledTimes(1)` and `.toHaveBeenCalledWith('mi-slug', { silent: true })` | Current code asserts `not.toHaveBeenCalled()`; the new expectation is its exact inverse |
| 2 | `replaces the state post with the fetched copy on resolve` | hook; deferred resolving to `buildPost({ title: 'Post del servidor', views: 8 })` | After `act(resolve)`: `findByText('Post del servidor')`; `queryByText('Post desde el estado')` is `null`; `queryByRole('status')` stayed `null` throughout | No fetch happens today, so the state title never changes |
| 3 | `keeps showing the state post and raises no alert when the background GET fails` | hook; `getBlogPostsById` → `mockResolvedValue(undefined)` (matches the real silent catch) + `vi.mock('sweetalert2')` spy on `Swal.fire` | `findByText('Post desde el estado')` still present; `queryByText('Publicación no encontrada')` is `null`; `Swal.fire` not called; call args include `{ silent: true }` | Fetch is skipped today; and a naive `setPost(fetched ?? undefined)` implementation makes this RED by blanking the page — this is the test that pins Decision 4 |
| 4a | **keep** `fetches the post by id when no router state is present` | hook | unchanged; also add `toHaveBeenCalledTimes(1)` | Regression guard — must stay GREEN before and after |
| 4b | **keep** `shows a not-found page when the fetched post does not exist` | hook | unchanged | Regression guard for the cold 404 path (Decision 4's second arm) |
| 4c | **new** `shows the skeleton while the cold fetch is in flight` | hook; deferred | Before resolve `getByRole('status')` present; after resolve absent | Regression guard: proves Test 1's "no skeleton" is a real discriminator and not a broken query |
| 5 | **new** `issues one network GET per mount, even across remounts` | `vi.mock('@/modules/core/utils/axiosUtils')` → `sendGet`; real `QueryClientProvider` (one shared `new QueryClient()`), `ErrorProvider`, `BlogPostProvider`; wrapped in `<React.StrictMode>` | Mount 1 (StrictMode) → `expect(sendGet).toHaveBeenCalledTimes(1)`; `unmount()`; mount 2 → `toHaveBeenCalledTimes(2)` | Zero calls today. Locks **both** Decision 2 (StrictMode dedupe holds — count 1, not 2) and Decision 5 (`staleTime 0` — the second mount is not served from cache) |

Test 5 is the load-bearing one and the reason no counted-ref guard is added. It
lives in `BlogPostDetailPage.test.tsx` in its own `describe` block, since its
mock surface (axios) differs from the rest of the file (hook).

**How "exactly one GET" is asserted at two levels, deliberately:**

- Component contract (Tests 1, 4a): `getBlogPostsById` called once per mount.
  RTL does not wrap in `StrictMode`, so this measures the component's own intent.
- Network contract (Test 5): `sendGet` called once per mount *under* StrictMode.
  This measures what actually reaches the server, which is what `Views` counts.

**How "no skeleton" is asserted:** `DetailSkeleton` renders
`<Box role="status" aria-label="Cargando" aria-busy="true">`
(`DetailSkeleton.tsx:13`), already relied on by `skeletons.test.tsx:15,33`.
`expect(screen.queryByRole('status')).not.toBeInTheDocument()` is therefore a
precise probe. Checking it while the deferred promise is still pending — rather
than after `waitFor` — is what makes it a *flash* test instead of an end-state
test.

Manual verification before merge: as an anonymous user open a post via "Leer más",
confirm `Vistas` in `/panel` increments by exactly 1; repeat via admin "Ver" and
confirm it does **not** increment.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Club12-WebClient/src/views/blogPost/BlogPostDetailPage.tsx:1,30-55` | Modify | Import `useRef`; `seededPost` extraction; `routeKeyRef`; guard → `if (!idOrSlug) return;`; conditional `setLoading(true)`; `cancelled` cleanup; conditional `setPost`; deps → `[idOrSlug, getBlogPostsById]`; comments replaced |
| `Club12-WebClient/src/views/blogPost/BlogPostDetailPage.test.tsx` | Modify | Test 1 rewritten (inverted assertion); Tests 2, 3, 4c added; Test 5 added in a second `describe` with axios-level mocks; `deferred` helper |

No other file changes. `showPosts.tsx`, `BlogPostsPage.tsx`, `blogPost.hook.ts`,
`blogPost.context.tsx`, `blogPost.service.ts`, `queryKeys.ts`, `QueryProvider.tsx`
and the entire backend stay byte-identical.

## Threat Matrix

| Boundary | Applicable? | Expected safe behaviour | RED test |
|---|---|---|---|
| Untrusted URL param → request path | **Applicable** | `idOrSlug` is interpolated into the resource path by `blogPostService.getBlogPostsById` and encoded by axios; the backend resolves it as an id-or-slug lookup and 404s otherwise. This change does not widen the input surface — the same param already reached the same call on the direct-URL path | Test 4b (unknown id → 404) covers the non-matching path |
| Rendering fetched HTML | **Pre-existing, unchanged** | `dangerouslySetInnerHTML={{ __html: post.markdownText }}` (`:113`) already renders server HTML. After this change the rendered HTML comes from the **server** instead of from `location.state` — strictly *less* client-controllable, since `location.state` is attacker-influenceable by any code able to call `navigate` | Out of scope; no regression introduced. Flag as a standing follow-up (sanitise on the server or via DOMPurify) |
| Authorization / counting rules | **Applicable** | Increment eligibility is decided server-side from the JWT role (`includeUnpublished`), never from a client flag. Admin "Ver" therefore stays uncounted with zero client work | Existing backend `BlogPostViewCounterTests` (unchanged, must stay green) |
| Denial of service / amplification | **Applicable, bounded** | One extra `GET` + one `UPDATE` per in-app post open. `fetchQuery` dedupes concurrent same-key calls, and the `post`-out-of-deps rule removes the only unbounded-loop vector | Test 1 (`toHaveBeenCalledTimes(1)`) and Test 5 (network count) |
| Shell / subprocess | N/A | No process spawned | — |
| File / upload handling | N/A | No file handling | — |
| VCS/PR automation | N/A | None added | — |
| Secrets | N/A | No credential handling; the GET is `[AllowAnonymous]` | — |

## Migration / Rollout

No schema, data, config or backend change. Deploy the client bundle. Rollback is
`git revert` of the single commit; the counter returns to direct-URL-only
behaviour and already-recorded `Views` values remain valid. There is no stored
state to unwind and no coordination with a backend release.

Observable effect after deploy: `Views` starts climbing at a genuinely higher
rate. That is the fix, not a bug — but it means historical `Views` are *not*
comparable across the deploy boundary. Worth one line in the release note so
nobody reads the step change as data corruption.

## Review Budget

| Area | Authored changed lines (est.) |
|---|---|
| `BlogPostDetailPage.tsx` | ~35 (incl. comments) |
| `BlogPostDetailPage.test.tsx` | ~150 |
| **Total** | **~185** |

Within the 400-line default budget and far within the 1500-line budget named for
this change. `400-line budget risk: Low`. **Single PR**, no chaining.

## Open Questions — RESOLVED

- [x] **404 on the background GET** (deferred from the proposal): keep showing
      `state.post`; fall to the not-found branch only on the cold path
      (Decision 4). The data layer cannot distinguish 404 from a transient
      failure, so tearing down readable content is not justified.
- [x] **Counted-ref guard**: not added; `fetchQuery` dedupe is relied upon and
      the reliance is asserted by Test 5 (Decision 2). Contingency recorded.
- [x] **`staleTime` assertion**: behavioural, via Test 5. `QueryProvider.tsx` is
      not touched and its config is not asserted directly (Decision 5).

## Follow-ups (not this change)

1. Surface the HTTP status from `getBlogPostsById` (e.g. return a discriminated
   result instead of `undefined`) so a genuine 404 can be distinguished from a
   transient failure. That would let Decision 4 be revisited on evidence.
2. Make the `staleTime` dependency explicit at the data layer, e.g. pass
   `staleTime: 0` on the `blogPostKeys.byId` `fetchQuery` call so the coupling
   survives a future global default change. Deliberately deferred: it edits
   `blogPost.context.tsx`, which this change keeps untouched.
3. Sanitise `markdownText` before `dangerouslySetInnerHTML` (pre-existing).

## Key Learnings

1. The `post` state variable must be removed from the `useEffect` dependency
   array in the same edit that removes the `if (post) return;` guard, because
   `getBlogPostsById` returns a new object on every network round-trip and would
   otherwise re-trigger the effect indefinitely, issuing one `GET` and one
   `Views++` per iteration.
2. `getBlogPostsById` has a stable identity across `ErrorProvider` re-renders
   because its `useCallback` depends only on `queryClient` and
   `handleUnknownError`, and the latter transitively depends only on the
   dependency-free `setMessage` callback — unlike the sibling `BlogPostProvider`
   callbacks, which close over `useMutation` objects that change every render.
3. `queryClient.fetchQuery` registers its in-flight promise synchronously before
   `getBlogPostsById` awaits, so React StrictMode's same-commit double invocation
   joins the existing request and produces exactly one network call and one view
   increment even against a real dev backend.
4. `getBlogPostsById` returns `undefined` for every failure class — 404, 500,
   timeout and offline alike — because `blogPost.context.tsx` catches the axios
   error and, under `{ silent: true }`, returns without rethrowing, so the
   component cannot distinguish a deleted post from a dropped connection.
5. `DetailSkeleton` renders a `role="status"` element, which makes
   `queryByRole('status')` a precise probe for skeleton absence, and asserting it
   while a deferred fetch promise is still pending is what turns an end-state
   check into a genuine no-flash check.
