# Proposal: Structural Refactor — Auth Boundary + TeamsPage Split

## Intent

Close two structural debts surfaced by the clean-architecture audit and re-verified against current code:

1. **API→Infrastructure leak.** `AuthController` (line 20) injects `UserManager<ApplicationUser>` and its `Logout` action (lines 93–107) manipulates `ApplicationUser.RefreshToken` directly. Every other action routes through `IAuthenticationService`. This is the only place the API layer touches Identity types directly.
2. **No container/presentational split.** `TeamsPage.tsx` (602 lines, unchanged) owns data fetching, filter+debounce+pagination state, create/edit dialog forms, delete confirmation, and rendering in one component. No view in the codebase models the split; this slice establishes the pattern once.

Pure structural work — zero behavior change intended.

## Scope

### In Scope
- Add `LogoutAsync(Guid userId, CancellationToken)` to `IAuthenticationService`; implement in `IdentityAuthenticationService` (already holds `UserManager`).
- Route `AuthController.Logout` through the interface; drop `UserManager`/`ApplicationUser`/`Infrastructure.Identity` from the controller.
- Backend regression test proving Logout is identical before/after (204 response; refresh token + expiry cleared; missing-user no-op).
- Decompose `TeamsPage.tsx` into a container (data/state/handlers) + presentational parts (filter bar, data grid, create dialog, edit dialog).
- Frontend tests proving filtering, pagination, and create/edit/delete flows behave identically.

### Out of Scope
- No new features, no API contract change, no visual/UX change to TeamsPage.
- No refactor of other `views/*Page.tsx` files (later slices reuse this pattern).
- No changes to `useTeam` hook or team module APIs.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
None — behavior is preserved; this is an internal structural refactor with no spec-level requirement change.

## Approach

Backend: move the refresh-token-clearing side effect down into the existing Infrastructure implementation, exposing it through the Application interface — the same pattern all other auth actions use. Frontend: extract a `TeamsPageContainer` owning hooks/state/handlers, passing props to stateless presentational children mirroring current JSX. Follow existing MUI/DataGrid conventions; no new libraries.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `API/Controllers/AuthController.cs` | Modified | Remove `UserManager`, call `LogoutAsync` |
| `Application/Interfaces/Services/IAuthenticationService.cs` | Modified | Add `LogoutAsync` |
| `Infrastructure/Identity/IdentityAuthenticationService.cs` | Modified | Implement `LogoutAsync` |
| `views/team/TeamsPage.tsx` + new child components | New/Modified | Container + presentational split |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Logout side effects drift | Low | Regression test locks response + token state |
| TeamsPage behavior regression | Med | Behavior tests over filter/paginate/CRUD flows |
| Combined change exceeds 800-line budget | High | **Split into two changes**: (A) small backend auth fix, (B) larger TeamsPage decomposition |

## Rollback Plan

Each change is an isolated commit. Revert the backend commit to restore direct `UserManager` injection; revert the frontend commit(s) to restore the monolith. No data/migration impact.

## Dependencies

- Depends on the 4 prior remediation batches (test scaffolding, error-handling fixes) already landed on `develop`.

## Success Criteria

- [ ] `AuthController` no longer references `UserManager`/`ApplicationUser`.
- [ ] Logout regression test passes with identical response + token side effects.
- [ ] `TeamsPage.tsx` reduced to a thin container delegating to presentational children.
- [ ] Frontend tests confirm identical filter/pagination/CRUD behavior.
- [ ] Combined delivery respects 800-line budget (split if exceeded).
