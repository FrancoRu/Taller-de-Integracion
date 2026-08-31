```yaml
schema: gentle-ai.verify-result/v1
evidence_revision: sha256:d19904a932b60944e936e3d9935774227070d745550c0ee4fa5babbde0236435
verdict: pass_with_warnings
blockers: 0
critical_findings: 0
requirements: 7/7
scenarios: 7/7
test_command: npm run test --prefix Club12-WebClient
test_exit_code: 0
test_output_hash: sha256:c88b0914f86e24d23b95bf1798562b0c48d8cfb90b2b3928976854343f2a96e6
build_command: cd Club12-WebClient && npx eslint src/views/blogPost --max-warnings 0 && npx tsc --noEmit
build_exit_code: 0
build_output_hash: sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
```

## Verification Report

**Change**: fix-blog-view-counter
**Version**: blog-post-view-counter spec (new capability, v1)
**Mode**: Strict TDD
**Commit**: 0d7eda9 on fix/blog-view-counter (PR #91 to develop)

### Completeness
| Metric | Value |
|--------|-------|
| Tasks total | 13 |
| Tasks complete | 12 |
| Tasks incomplete | 1 (task 5.3 manual browser check, needs a running stack) |

All Phase 1-5 checkboxes are checked except task 5.3, a manual browser/E2E
verification that cannot run in this environment. Automated proxies (Test 1,
Test 5, the two StrictMode/remount tests, backend BlogPostViewCounterTests) are
green.

### Build & Tests Execution
**Build**: PASS. Command: eslint src/views/blogPost --max-warnings 0 && tsc --noEmit.
Exit 0, no output, no exhaustive-deps suppression.

**Tests**: PASS. 503 passed / 0 failed / 0 skipped.
Test Files 106 passed (106); Tests 503 passed (503).
BlogPostDetailPage.test.tsx: 9/9 passed (8 main describe + 1 network-boundary).

Backend: NOT executed by design. git show --name-only 0d7eda9 touches zero
Club12-Backend files (verified). Apply run recorded 728/728 backend green.

**Coverage**: Not available (no coverage tool configured in the vitest run).

### Spec Compliance Matrix
| Requirement | Scenario | Test | Result |
|-------------|----------|------|--------|
| In-App Read Increments the View Counter | Leer mas from the list counts one view | BlogPostDetailPage.test.tsx: renders the post from router state and still fires exactly one background GET; plus issues one network GET per mount, even across remounts | COMPLIANT |
| Direct-URL Read Increments the View Counter | Direct URL open counts one view | BlogPostDetailPage.test.tsx: fetches the post by id when no router state is present (asserts 1 call, silent true) | COMPLIANT |
| Admin and Owner Reads Do Not Increment | Admin Ver does not count | Server-side invariant (includeUnpublished from JWT role); client issues the same GET on every path, verified NO client change via git show 0d7eda9. Backend BlogPostViewCounterTests green in apply run, not re-executed here (zero backend files changed). | COMPLIANT (by invariant, see WARNING 1) |
| Instant Render From Router-State Post | No skeleton flash on the state-present path | BlogPostDetailPage.test.tsx: renders the post from router state (role=status query null while pending); plus replaces the state post (null throughout); plus shows the skeleton while the cold fetch is in flight | COMPLIANT |
| Fetched Server Copy Replaces the Displayed Post | Resolve swaps in server content | BlogPostDetailPage.test.tsx: replaces the state post with the fetched copy on resolve | COMPLIANT |
| Silent Failure Keeps the Router-State Post | Background GET fails | BlogPostDetailPage.test.tsx: keeps showing the state post and raises no alert when the background GET fails (state post stays, no not-found text, Swal.fire not called) | COMPLIANT |
| Every Qualifying Load Counts | Back to list then reopen counts twice | BlogPostDetailPage.test.tsx: fires the background GET again after a real remount for the same slug (count 2 across unmount/remount); plus issues one network GET per mount, even across remounts (sendGet 2x) | COMPLIANT |

**Compliance summary**: 7/7 scenarios compliant.

### Correctness (Static Evidence)
| Requirement | Status | Notes |
|------------|--------|-------|
| Fire GET on router-state path | Implemented | Guard relaxed to: if (!idOrSlug || requestedForRef.current === idOrSlug) return; getBlogPostsById(requestedFor, silent true) fires unconditionally. |
| No skeleton flash when seeded | Implemented | loading init !seededPost; routeKeyRef seeded to idOrSlug, so setLoading(true) is skipped on the warm path. |
| Server copy swap | Implemented | if (fetchedPost) then setPost(fetchedPost) and routeKeyRef.current = requestedFor. |
| Silent failure keeps content | Implemented | silent true passed; on undefined, warm path does not setPost(undefined). |
| Cold 404 preserved | Implemented | else if (routeKeyRef.current !== requestedFor) setPost(undefined) then 404 branch. Test 4b green. |
| No unbounded fetch loop | Implemented | post absent from deps [idOrSlug, getBlogPostsById]; requestedForRef also short-circuits provider re-renders. |
| StrictMode dev double-count fixed | Implemented | requestedForRef fetches at most once per idOrSlug per mount; real remount = fresh ref = counts again. |
| Requirement 7 survives the guard | Verified | requestedForRef is a per-instance useRef(undefined); only collapses the synthetic StrictMode double-invoke, never a real navigation/reopen. Remount test proves it. |
| Backend untouched | Verified | git show --name-only 0d7eda9 lists 0 Club12-Backend files. |

### Coherence (Design)
| Decision | Followed? | Notes |
|----------|-----------|-------|
| 1: routeKeyRef tracks displayed route, not state | Yes | Present, seeded seededPost ? idOrSlug : undefined. |
| 2: rely on fetchQuery dedupe, no counted-ref [REVERSED after QA] | Yes, reversal implemented | requestedForRef guard shipped. Reversal recorded consistently in design.md (struck-through Decision 2 row plus QA UPDATE callout) AND apply-progress.md Batch 2. Consistent. |
| 3: cancelled cleanup flag, not AbortController | Superseded | cancelled flag removed in Batch 2 (it deadlocked the cold-path skeleton against the ref guard); stale-response protection is now the requestedForRef.current check inside loadPost. Behaviourally equivalent for the race it guarded. Recorded in Batch 2 and the Decision 2 callout. |
| 4: failed/empty GET keeps state.post; 404 only on cold path | Yes | else if (routeKeyRef.current !== requestedFor) arm. Test 3 plus Test 4b. |
| 5: assert staleTime coupling behaviourally, not by reading config | Yes | Test 5 = mount, unmount, mount, expects 2 network GETs; QueryProvider.tsx untouched. Coupling comment present at fetch site. |
| 6: single loading boolean, no refreshing flag | Yes | One useState(!seededPost). |

### TDD Compliance
| Check | Result | Details |
|-------|--------|---------|
| TDD Evidence reported | Yes | apply-progress.md has a TDD Cycle Evidence table (Batch 1) plus prose RED/GREEN evidence for the 2 Batch 2 tests |
| All tasks have tests | Yes | 7 Batch-1 test rows plus 2 Batch-2 tests, all mapped to tasks/requirements |
| RED confirmed (tests exist) | Yes | 9/9 tests present; documented RED signals (0 calls vs expect 1; got 2 times for the Batch-2 StrictMode test) |
| GREEN confirmed (tests pass) | Yes | 9/9 pass on re-execution; full suite 503/503 |
| Triangulation adequate | Yes | exactly-one-GET asserted at both the component layer (Tests 1, 4a) and the network layer under StrictMode (Test 5); warm vs cold path paired |
| Safety Net for modified files | Yes | BlogPostDetailPage.test.tsx pre-existing, extended not replaced; full suite run before impl |

**TDD Compliance**: 6/6 checks passed. Minor: Batch 2 two tests documented in prose, not appended to the evidence table (WARNING 3).

### Test Layer Distribution
| Layer | Tests | Files | Tools |
|-------|-------|-------|-------|
| Unit | 0 | 0 | -- |
| Integration (RTL render plus real Router; one with real QueryClient/ErrorProvider/BlogPostProvider under StrictMode) | 9 | 1 | vitest + testing-library/react |
| E2E | 0 | 0 | not installed (task 5.3 is the manual E2E stand-in) |
| Total | 9 | 1 | |

### Changed File Coverage
Coverage analysis skipped, no coverage tool detected in the configured vitest run.

### Assertion Quality
All assertions verify real behavior. No tautologies, no orphan empty-checks, no
ghost loops. The toHaveBeenCalledTimes and toHaveBeenCalledWith(idOrSlug, silent true)
assertions are the literal spec contract (exactly one GET, silent mode), not
implementation coupling. Negative assertions (Swal.fire not called, role=status
query null) are each paired with a positive content assertion in the same test.

### Quality Metrics
**Linter**: No errors, no warnings (eslint --max-warnings 0 exit 0).
**Type Checker**: No errors (tsc --noEmit exit 0).

### Issues Found

**CRITICAL**: None.

**WARNING**:
1. Requirement 3 (Admin/Owner no increment) is not re-executed at runtime in this
   verify pass. Its covering test is the backend BlogPostViewCounterTests, and the
   backend suite was deliberately skipped (zero backend files in 0d7eda9).
   Mitigation: the requirement is satisfied purely by the server-side
   includeUnpublished rule and the client provably makes no change on that path
   (git show --name-only 0d7eda9). Apply run recorded backend 728/728.
2. design.md internal drift: the Code sketch before/after (After block), both
   sequence diagrams, and the File Changes table still describe the superseded
   cancelled-flag implementation. The reversal of Decision 2 IS recorded
   consistently at the top of the file (struck-through Decision 2 row plus the
   StrictMode double-invoke QA UPDATE section) and in apply-progress.md Batch 2,
   so the task-specified consistency check passes, but the design body was not
   fully re-synced to the shipped requestedForRef code. Documentation-only;
   implementation is correct.
3. tasks.md task 3.1 and 5.2 text still reflects Batch 1 (cancelled flag, 501/501).
   Current state is requestedForRef and 503/503. Superseded by apply-progress.md
   Batch 2 but the task file was not updated in place.

**SUGGESTION**:
1. Task 5.3 (manual browser check) remains the only open item. Run it once a stack
   is up: anon Leer mas then /panel Vistas +1 exactly; admin Ver then no change;
   DevTools Network shows exactly 1 GET per visit in dev.
2. Design follow-ups still open (not this change): surface HTTP status from
   getBlogPostsById to distinguish 404 from transient; pass explicit staleTime 0
   on the byId fetchQuery; sanitise markdownText before dangerouslySetInnerHTML.
3. When archiving, refresh the design.md body and tasks.md 3.1/5.2 to the shipped
   requestedForRef implementation so the archived artifacts are self-consistent.

### Verdict
**PASS WITH WARNINGS**. All 7 spec requirements and 7 scenarios are implemented and
covered by passing runtime tests (503/503 frontend; lint and typecheck clean;
backend byte-identical). The requestedForRef guard added post-QA fixes the
StrictMode dev double-count without breaking requirement 7 (real remount = fresh
ref = fresh count), confirmed by a dedicated test. Warnings are documentation
drift in design.md/tasks.md and one manual E2E check (5.3) that needs a running
stack. None block archive.

## Key Learnings

1. The requestedForRef fetch-once-per-idOrSlug guard collapses only React StrictMode same-instance double effect-invoke, so a real route unmount resets the ref and every genuine reopen still increments the counter.
2. jsdom runs StrictMode double effect-invoke synchronously, so react-query in-flight dedupe hid the browser real second GET in tests, and the network-boundary test mocking axios sendGet is what now locks the once-per-mount contract.
3. Removing the cancelled cleanup flag was required because, combined with an early-returning second StrictMode setup, it left the cold-path skeleton stuck, so stale-response protection moved to a requestedForRef.current check inside the async body.
4. Requirement 3 needs zero client work because Admin and Owner increment eligibility is decided server-side from the JWT role via includeUnpublished, and the commit provably touches no backend file.
5. The Decision 2 reversal is recorded consistently in both design.md and apply-progress.md Batch 2, but the design.md code sketch and tasks.md task 3.1 were not re-synced to the shipped implementation.
