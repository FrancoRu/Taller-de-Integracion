# Proposal: Codebase Clean-Architecture Audit — Verdict + Test Scaffolding

## Intent

Exploration found 45+ files with real issues (400-vs-404 bugs, a frontend 401-redirect gap, magic strings/numbers, dead code, missing container/presentational split) but **no test runner on either side**. With Strict TDD enabled, no behavior-changing fix is safely verifiable today. Fixing everything in one PR would blow the 800-line budget and carry high regression risk. This change formalizes the audit verdict and stands up the minimal test scaffolding that unblocks every later fix.

## Scope

### In Scope
- Formalize the audit verdict as a tracked deliverable (findings + prioritized remediation program below).
- Backend test scaffolding: xUnit test project + `WebApplicationFactory` integration harness, one smoke test proving the pipeline runs.
- Frontend test scaffolding: Vitest + Testing Library + jsdom config, one smoke test proving the pipeline runs.
- Document the recommended follow-on change sequence (each independently sized under 800 lines).

### Out of Scope (Non-Goals)
- No business-logic changes; no public API contract changes.
- No 400→404 status fixes, no `sendGet` pipeline fix, no color/string extraction, no dead-code removal, no refactors — all deferred to follow-on changes.
- No i18n wiring, no env-based URL config.

## Recommended Follow-On Program
1. **(this change)** Test scaffolding — prerequisite for all below.
2. **Mechanical / behavior-preserving** — magic strings/numbers, dead-code removal, param-naming consistency. Covered by scaffolding; no new behavior tests.
3. **Behavior-changing (dedicated tests)** — 400→404 across controllers *coordinated with* frontend status handling; `sendGet` 401-pipeline fix with regression test.
4. **Structural refactors (per-domain)** — `TeamsPage` container/presentational decomposition, `AuthController` boundary fix, query-key factory — incrementally, one domain per slice.

## Capabilities

### New Capabilities
- None (scaffolding + audit; no product behavior introduced).

### Modified Capabilities
- None (no spec-level behavior changes in this change).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Club12-Backend/Solution/` | New | xUnit project + WebApplicationFactory harness + smoke test |
| `Club12-WebClient/` | New/Modified | Vitest+RTL config, `package.json` devDeps/scripts, smoke test |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| TDD blocker: no coverage for later fixes | High | This change delivers the scaffolding first, before any behavior change |
| 400→404 is a breaking API-contract change | Med | Deferred to a single coordinated BE+FE slice with dedicated tests |
| Scope creep across 45+ files | High | Strict non-goals; remediation split into independently-budgeted slices |
| Scaffolding choice mismatches team tooling | Low | Use ecosystem defaults (xUnit, Vitest); no app code touched |

## Rollback Plan

Scaffolding is additive (new test project + config). Revert by deleting the test project and reverting `package.json`; zero production code changes, so no runtime rollback needed.

## Dependencies

- None external. Backend needs `Microsoft.AspNetCore.Mvc.Testing`; frontend needs `vitest`, `@testing-library/react`, `jsdom`.

## Success Criteria

- [ ] Backend `dotnet test` runs green with a passing smoke test.
- [ ] Frontend `vitest run` runs green with a passing smoke test.
- [ ] Audit verdict + follow-on program recorded as the tracked deliverable.
- [ ] Zero production/business-logic files modified in this change.
