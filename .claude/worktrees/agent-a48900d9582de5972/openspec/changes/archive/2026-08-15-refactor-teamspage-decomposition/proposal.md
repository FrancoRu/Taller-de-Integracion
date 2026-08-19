# Proposal: TeamsPage Decomposition (container/presentational split)

## Intent

`Club12-WebClient/src/views/team/TeamsPage.tsx` (602 lines) fuses data fetching,
debounced filter state, pagination, create/edit dialog forms, and delete
confirmation into one monolith with no container/presentational split. This is
the largest untestable view in the frontend. Decomposing it into a container plus
stateless children makes each flow independently testable and the file readable.
Payoff is purely maintainability and testability — **zero behavior change, zero
visual change**. Extracted from the archived sibling change (Slice B); Slice A
(auth boundary) already shipped.

## Scope

### In Scope
- Split `TeamsPage.tsx` into a container + presentational children per the reused design.
- Create `teams.types.ts`, `TeamsFilterBar.tsx`, `TeamsTable.tsx`, `TeamFormDialog.tsx` (flat siblings under `views/team/`).
- Container keeps default export + `TeamsScreenProps` (zero import churn for `App.tsx`/`TournamentPage.tsx`).
- RTL behavior suite over the 8 spec scenarios (filter/debounce, pagination, create, edit, delete).

### Out of Scope
- Any other `views/*Page.tsx` file.
- New features, visual/UX changes, or restyling.
- Changes to `useTeam` hook, `team.context.tsx`, or team module API/service layer.
- Backend (Slice A already delivered separately).

## Capabilities

### New Capabilities
- None (behavior-preserving refactor; the spec locks existing behavior).

### Modified Capabilities
- None at the spec level. `teamspage-decomposition/spec.md` is a characterization
  spec: every scenario MUST behave identically before and after.

## Approach

Container (`TeamsPage.tsx`) owns all state, `useTeam()`, effects, and handlers;
children are pure props-in. One reusable `TeamFormDialog` with a `withLogo` prop
serves create + edit; delete stays an imperative Swal handler. JSX/`sx` moved verbatim.

**Delivery — chained PRs (est. ~900-1050 lines exceeds 800 budget):**
- **PR 1 — structure + safety net:** bootstrap FE test harness (vitest + RTL, if
  absent), add the behavior suite characterizing the *current* monolith (passes as-is),
  create `teams.types.ts` + the 3 presentational files as standalone, self-tested
  modules (not yet wired). Builds green; container unchanged.
- **PR 2 — wiring:** rewrite the container to consume the new components and delete
  the duplicated JSX. Same behavior suite still passes over the decomposed tree.

Each PR independently builds and passes tests — no broken intermediate state on `develop`.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `views/team/TeamsPage.tsx` | Modified | Reduced to container; export unchanged |
| `views/team/teams.types.ts` | New | Shared local types |
| `views/team/TeamsFilterBar.tsx` | New | Filter fields |
| `views/team/TeamsTable.tsx` | New | DataGrid |
| `views/team/TeamFormDialog.tsx` | New | Reusable create/edit dialog |
| `views/team/*.test.tsx` | New | RTL behavior suite |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| No FE test runner configured (`openspec/config.yaml`) | High | PR 1 bootstraps vitest + RTL as a prerequisite step |
| Hidden behavior drift during JSX move | Med | Characterization suite passes on monolith first, then post-split |
| Prop-drilling churn breaks callers | Low | Default export + `TeamsScreenProps` preserved verbatim |

## Rollback Plan

Each PR is an isolated commit. Revert PR 2 restores the monolith container; revert
PR 1 removes the new files and test harness. No data/schema/API impact.

## Dependencies

- FE test harness (vitest + React Testing Library) must exist before the behavior
  suite — bootstrapped in PR 1 if not already present.

## Success Criteria

- [ ] All 8 spec scenarios pass identically pre- and post-decomposition.
- [ ] `npm run build` succeeds after each PR.
- [ ] No `views/*Page.tsx` file other than `TeamsPage.tsx` changes.
- [ ] No visual/CSS/`sx`/theme diff.
