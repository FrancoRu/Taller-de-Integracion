# Archive Report: Blog Post View Counter Increments on In-App Reads

**Change**: `fix-blog-view-counter`  
**Archive date**: 2026-08-30  
**Status**: SHIPPED TO PRODUCTION  
**Verify verdict**: PASS WITH WARNINGS (0 CRITICAL)

## Final State Authority

This report reflects the state AT CLOSE of the SDD cycle, per the final-state authority hierarchy. The change is shipped to production and user-verified. Intermediate snapshots (`verify-report` and `apply-progress`) are historical records; explicit final-state facts from the user's confirmation (shipping and manual testing) outrank any stale intermediate claims.

## Executive Summary

The blog-post view counter now increments on every in-app read through a frontend-only fix: `BlogPostDetailPage.tsx` always fires the background `GET /api/blogposts/{idOrSlug}` on the router-state path, guarded by a `requestedForRef` to prevent React StrictMode's dev double-mount from double-counting. A parallel fix to `home.tsx` removes its pre-fetch so the detail page becomes the single owner of the `GET` trigger for all public entry points (Novedades, home, direct URL). Shipped in two PRs merged to `develop` (PR #91 + PR #92, commits 5f4be40 and 06762d2), verified in production, counter now accurate.

## Shipped Artifacts

### PR #91 — BlogPostDetailPage `requestedForRef` guard (Batch 1 + Batch 2)

**Target branch**: `develop`  
**Merge commit**: `5f4be40`  
**Implementation commit**: `0d7eda9` (contains both Batch 1 and Batch 2 fixes)  
**Files**:
- `Club12-WebClient/src/views/blogPost/BlogPostDetailPage.tsx` — guard changed from `if (post || !idOrSlug) return;` to `if (!idOrSlug || requestedForRef.current === idOrSlug) return;`; added `requestedForRef` to fetch at most once per `idOrSlug` per mount; removed `cancelled` cleanup flag (Batch 2 superseded it).
- `Club12-WebClient/src/views/blogPost/BlogPostDetailPage.test.tsx` — 9 tests: Test 1 rewritten to expect exactly-one background GET on router-state path; Tests 2–4c new (silent failure, server copy swap, skeleton during cold fetch); Test 5 new at axios boundary under StrictMode (verifies one network GET per mount, two across remount).

**What it fixes**: Router-state path never fired `GET /api/blogposts/{slug}`, so "Leer más" and admin "Ver" never incremented `Views`. Batch 1 adds the unconditional background fetch. Batch 2 adds `requestedForRef` guard after QA found the browser's real GET gap during React StrictMode's dev double-mount (jsdom had synchronous dedupe; browser did not), preventing double-counts.

### PR #92 — Home page pre-fetch removal (Batch 3)

**Target branch**: `develop`  
**Merge commit**: `06762d2`  
**Files**:
- `Club12-WebClient/src/views/home/home.tsx` — `handleReadMore(post)` now navigates with the already-loaded post from the list fetch, no pre-fetch.
- `Club12-WebClient/src/views/home/home.test.tsx` (new) — test for no pre-fetch before navigation.

**What it fixes**: Home "Últimas noticias" section pre-fetched before navigating, so users saw two increments per visit (pre-fetch + detail-page fetch). Batch 3 removes the pre-fetch so home matches the Novedades path (navigate with in-memory post, detail page owns the `GET`).

**Note**: The "Novedades" entry point (`showPosts.tsx`) did not pre-fetch and required no change.

## Implementation Batches

| Batch | Trigger | Issue | Fix | Commit | PR |
|-------|---------|-------|-----|--------|---|
| **1** | Initial design | Router-state path never fires GET → Vistas flat for in-app opens | Always fetch, render from state; `{ silent: true }` on failure | Included in `0d7eda9` | #91 |
| **2** | QA manual test (`npm run dev`) | Counter moves "de a 2" per visit in dev; DevTools shows 2 GETs | Add `requestedForRef` guard (fetch ≤1× per idOrSlug per mount) | Included in `0d7eda9` | #91 |
| **3** | Post-QA grep for `getBlogPostsById` in frontend | Home "Últimas noticias" pre-fetches → second increment | Remove pre-fetch from home.tsx; detail page is single owner | `06762d2` | #92 |

## Verification Results

**Verdict**: PASS WITH WARNINGS (0 CRITICAL)

### Spec Compliance

| Requirement | Scenario | Result |
|-------------|----------|--------|
| In-App Read Increments | "Leer más" from list counts one view | COMPLIANT (Test 1, Batch 1) |
| In-App Read Increments | "Últimas noticias" card counts one view | COMPLIANT (Batch 3 removes pre-fetch) |
| Direct-URL Read Increments | Direct URL open counts one view | COMPLIANT (Test 4a, unchanged) |
| Admin/Owner Reads Do Not Increment | Admin "Ver" does not count | COMPLIANT (server-side JWT guard, zero client change) |
| Instant Render From Router-State | No skeleton flash | COMPLIANT (Test 1, loading init `!seededPost`) |
| Fetched Server Copy Replaces | Resolve swaps in server content | COMPLIANT (Test 2) |
| Silent Failure Keeps Content | Background GET fails | COMPLIANT (Test 3, `{ silent: true }`) |
| Every Load Counts | Back-to-list then reopen counts twice | COMPLIANT (Test 5: 1 GET per mount, 2 across remount) |

**All 7 spec requirements and 7 scenarios**: implemented and verified.

### Test Summary

**Frontend suite**: 504/504 (107 files) — PASS  
**Backend suite**: 728/728 (byte-identical to before) — PASS per apply run  
**Lint**: eslint `--max-warnings 0` — PASS  
**Typecheck**: `tsc --noEmit` — PASS (no exhaustive-deps suppression)

**BlogPostDetailPage tests**:
- 9 tests total (1 rewritten, 3 new from Batch 1 + 2.1; 2 new from Batch 2; 2 kept)
- All 9 PASS (Batch 1: 6 in main describe; Batch 2: 2 new under `<StrictMode>`; Batch 3: covered by `home.test.tsx`)
- Test 1 + Test 5: exactly-one GET asserted at component layer and network layer
- Test 5 under `React.StrictMode` with real QueryClient: verifies dedupe holds despite browser gap

**Per apply-progress.md**:
- Batch 1: 501/501 frontend, Tests 1–5 GREEN
- Batch 2: 503/503 frontend, new StrictMode tests GREEN  
- Batch 3: 504/504 frontend (107 files), eslint + tsc clean

### User Manual Verification (Final State Authority)

**Source**: orchestrator launch prompt — "User confirmed the counter is correct in production"  
**Evidence**: opened posts via Novedades, home, and direct URL; `/panel` Vistas +1 exactly; admin "Ver" unchanged.  
**Task 5.3 status**: **NOW DONE** (was marked PENDING in apply-progress.md; user confirmed in production).

## Known Documentation Residue (No Block)

### design.md internal drift (WARNING from verify-report)

- **Issue**: Decision 2 was reversed (counted-ref guard added post-QA), and the reversal is recorded in the design's "StrictMode double-invoke — QA UPDATE" section and Decision 2 row (struck-through). However, the "Code sketch — before / after", both sequence diagrams, and the "File Changes" table still describe the superseded `cancelled` cleanup flag implementation.
- **Impact**: Documentation-only. The shipped code is correct (uses `requestedForRef`, no `cancelled`). Consistency check passes because the reversal is explicitly documented at the top of the design; the body was not fully re-synced.
- **Recommendation**: If the archived design doc is ever consulted, the reversal is documented; the "After" code sketch should be treated as the Batch 1 form, not the shipped Batch 2 form. See apply-progress.md Batch 2 for the canonical shipped code.

### tasks.md stale prose (WARNING from verify-report)

- **Issue**: Task 3.1 and 5.2 text still reflect Batch 1 (`cancelled` flag, 501/501 test count). Current state is `requestedForRef` and 504/504.
- **Impact**: Documentation-only. The task checkboxes are all marked complete; the prose is superseded by apply-progress.md Batch 2 and Batch 3.
- **Recommendation**: Refer to apply-progress.md Batch 2/3 for the final implementation and test counts.

### spec Non-Goals item

The spec lists a dedicated `POST .../views` increment endpoint as a Non-Goal and a deferred follow-up. This remains a valid follow-up (decoupling view-counting from the data `GET`), but it is not blocking the current shipped behavior.

## Entry-Point Matrix (After Batch 3)

Every public visit increments exactly once; admin visits do not:

| Entry point | Pre-fetch? | Detail `GET` | Server `Views++` | Notes |
|---|---|---|---|---|
| Novedades "Leer más" | no | 1 (Batch 1) | yes | Unchanged from current behavior |
| Home "Últimas noticias" | no (was yes) | 1 (Batch 1) | yes | Fixed by Batch 3 |
| Direct URL / refresh | n/a | 1 | yes | Unchanged |
| Admin "Ver" (any path) | no | 1 | no | Server-side JWT guard, zero client work needed |

## Rollback Plan (no change)

Revert commits `5f4be40` (PR #91, Batch 1+2) and `06762d2` (PR #92, Batch 3) — both frontend-only, no schema/data/backend state. The counter returns to direct-URL-only behavior. Already-recorded `Views` values remain valid.

## Follow-Ups (Not This Change)

1. **Surface HTTP status** from `getBlogPostsById` to distinguish 404 from transient failures (enables Decision 4 to be revisited).
2. **Explicit `staleTime` coupling** at the data layer: pass `staleTime: 0` on the `blogPostKeys.byId` `fetchQuery` call.
3. **Sanitise `markdownText`** before `dangerouslySetInnerHTML` (pre-existing security note).

## Archive Contents

All SDD artifacts preserved in this archive:

- `proposal.md` — intent, scope, risks, rollback, success criteria
- `specs/blog-post-view-counter/spec.md` — 7 requirements, 7 scenarios (new capability spec)
- `design.md` — technical approach, architecture decisions (Decision 2 reversal recorded), dependency analysis, sequence diagrams, code sketch, TDD strategy, threat matrix
- `tasks.md` — task checklist (all checked; prose stale for Batch 2+), review workload forecast (Low risk, single PR)
- `apply-progress.md` — TDD evidence, Batch 1/2/3 delivery history with RED/GREEN signals, deviations from design, no issues found
- `verify-report.md` — spec compliance matrix, test summary (504/504), TDD compliance, 0 CRITICAL, PASS WITH WARNINGS
- `exploration.md` — problem statement, root cause analysis (frontend guard), current state verification, decisions, affected areas
- `archive-report.md` — this file

## Cycle Summary

**Proposal phase**: one capability, one component, three new tests, minimal diff.  
**Spec phase**: 7 requirements, 7 scenarios, new blog-post read-path capability spec.  
**Design phase**: always-fetch pattern, `requestedForRef` guard (Batch 2), silent failure, dependency stability analysis, TDD strategy.  
**Apply phase**: Batch 1 (initial), Batch 2 (StrictMode fix post-QA), Batch 3 (home pre-fetch removal post-QA) — three focused changes, cumulative test count 504/504.  
**Verify phase**: PASS WITH WARNINGS (0 CRITICAL), manual user verification in production, all spec requirements compliant.  
**Archive phase**: specs synced, folder moved, cycle closed.

## Key Learnings

1. The `requestedForRef` guard collapses only React StrictMode same-instance double-invoke (per-mount cap), so a real route unmount resets the ref and every genuine reopen still increments the counter.
2. jsdom synchronously dedupes StrictMode double-invokes, but the real browser has a gap long enough for the first localhost GET to resolve and `staleTime` 0 to fire a second GET, so network-boundary tests (axios mocked) catch what component-layer tests miss.
3. Removing a state dependency from a `useEffect` must happen in the same edit as removing the guard that made it safe, or `staleTime` 0 produces an unbounded fetch loop.
4. Admin/Owner non-increment is preserved entirely server-side (JWT role via `includeUnpublished`), requiring zero client changes; front-end authorization decisions should be validated by code inspection, not re-tested at the component level.
5. Every public entry point (list "Leer más", home news card, direct URL) must route through a single owner (the detail page) for the `GET` trigger, and pre-fetches upstream silently double-increment until caught by systematic grep.
