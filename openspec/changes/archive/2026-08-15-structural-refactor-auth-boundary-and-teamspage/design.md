# Design: Structural Refactor — Auth Boundary + TeamsPage Split

## Technical Approach

Two independent, structure-only refactors delivered as two slices (A backend, B frontend), zero behavior/visual change. Verified against current code:
- **A** — `AuthController` is the only API action touching Identity (`UserManager<ApplicationUser>`). Move the refresh-token-clearing side effect into `IdentityAuthenticationService` (already holds `UserManager`) behind a new `IAuthenticationService.LogoutAsync`, matching every other auth action.
- **B** — `TeamsPage.tsx` (602 lines) mixes data/state/handlers with rendering. Extract a container (keeps `useTeam()` + all state/handlers) delegating to stateless presentational children. Data-fetching already lives in `team.context.tsx` (`TeamProvider`) via `useTeam()`; the container leans on it unchanged — no hook/module API change.

## Architecture Decisions

### A: LogoutAsync signature
| Option | Tradeoff | Decision |
|--------|----------|----------|
| `Task LogoutAsync(Guid userId, CancellationToken ct = default)` | Matches interface style (`ct = default`), no payload needed for 204 | **Chosen** |
| Return bool/DTO | Adds contract surface, no caller uses it | Rejected |
| Pass `ClaimsPrincipal` | Leaks web concern into Application | Rejected |

Controller resolves `id` via `User.GetCallerClaims()` and calls `LogoutAsync(id, ct)`. Implementation moves lines 99-105 verbatim (`FindByIdAsync(userId.ToString())` → clear `RefreshToken`/`RefreshTokenExpiryTime` → `UpdateAsync`), missing-user stays a silent no-op.

### B: Container/presentational boundary
Container (`TeamsPage.tsx`, unchanged default export + `TeamsScreenProps`) owns ALL state (`loading`, `submitting`, `filters`, `debouncedFilters`, `paginationModel`, `teamForm`, `isCreateModalOpen`, `editingTeam`), the `useTeam()` hook, debounce/fetch effects, and every handler/memo (`columns`, `teamActions`). Presentational children are pure props-in.

### B: File structure — flat co-location
| Option | Tradeoff | Decision |
|--------|----------|----------|
| Flat siblings under `views/team/` | Matches `team/` folder precedent (`TeamPage`, `TeamRegisterPage` already flat) and `scorer/`, `match/` tab-sibling precedent | **Chosen** |
| New `components/` subfolder | No precedent in this codebase | Rejected |

### B: Dialog reuse
Create and edit dialogs are near-identical; create adds a logo file field. One `TeamFormDialog` with a `withLogo` boolean renders the exact current JSX for both, cutting duplication without abstraction risk. Delete stays an imperative Swal handler in the container (not a component).

## Data Flow

    A:  AuthController.Logout ──(id, ct)──▶ IAuthenticationService.LogoutAsync
                                              └▶ IdentityAuthenticationService (UserManager)

    B:  TeamProvider (useTeam) ──▶ TeamsPage container (state + handlers)
             ▲                          │ props ▼
             └── mutations ──── TeamsFilterBar · TeamsTable · TeamFormDialog

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Application/Interfaces/Services/IAuthenticationService.cs` | Modify | Add `Task LogoutAsync(Guid userId, CancellationToken ct = default)` |
| `Infrastructure/Identity/IdentityAuthenticationService.cs` | Modify | Implement `LogoutAsync` (move controller side effect verbatim) |
| `API/Controllers/AuthController.cs` | Modify | Drop `UserManager`/`ApplicationUser`/`Infrastructure.Identity`/`Identity` usings; `Logout` becomes thin call |
| `views/team/TeamsPage.tsx` | Modify | Reduce to container; keep default export + `TeamsScreenProps` |
| `views/team/teams.types.ts` | Create | Shared local types (`TeamsSearchFilters`, `TeamFormState`) to avoid circular imports |
| `views/team/TeamsFilterBar.tsx` | Create | 3 filter `TextField`s, `onFilterChange` prop |
| `views/team/TeamsTable.tsx` | Create | `DataGrid` — `rows`, `columns`, `loading`, pagination props |
| `views/team/TeamFormDialog.tsx` | Create | Reusable create/edit dialog, `withLogo` prop |

## Interfaces / Contracts

```csharp
Task LogoutAsync(Guid userId, CancellationToken ct = default);
```

Frontend props are the container's existing values passed down verbatim (e.g. `TeamsTable`: `rows`, `columns`, `loading`, `noRowsMessage`, `paginationModel`, `onPaginationModelChange`, `pageSizeOptions`). No new module/hook API; `useTeam()` untouched.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit (BE) | `LogoutAsync` clears token+expiry; missing user = no-op | Mock `UserManager`, assert `UpdateAsync` |
| Integration (BE) | `POST /api/auth/logout` → 204, token cleared | Regression test locks response + side effect |
| Behavior (FE) | filter+debounce, pagination reset, create/edit/delete | RTL tests over container, identical to pre-refactor |
| Static (FE) | No CSS/`sx`/theme diff | Presentational JSX moved verbatim |

## Threat Matrix

N/A — no routing (route path unchanged), shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary.

## Migration / Rollout

No migration. Each slice is an isolated commit: revert A restores direct `UserManager` injection; revert B restores the monolith. No data/schema impact.

## Open Questions

- [ ] None blocking. Slice ordering is free (A and B are independent); recommend A first (smaller, unblocks reviewer budget).
