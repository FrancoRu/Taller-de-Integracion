# Tasks: TeamsPage Decomposition (Container/Presentational Split)

## Review Workload Forecast

### PR1 — new files + characterization suite (monolith untouched)

| Field | Value |
|-------|-------|
| Estimated changed lines | ~800–850 (8 new files: types + 3 components + 3 unit tests + 1 characterization suite; 0 deletions) |
| 400-line budget risk | High vs. 400 default, but measured against the agreed 800-line-per-PR budget: Medium (at/near ceiling) |
| Chained PRs recommended | Yes (already fixed as PR1→PR2) |
| Suggested split | PR1 (this) → PR2 |
| Delivery strategy | chained (user-fixed: two sequential PRs on `develop`) |
| Chain strategy | stacked-to-main |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Medium

### PR2 — wire container (delete duplicated JSX, rewire children)

| Field | Value |
|-------|-------|
| Estimated changed lines | ~260–300 (~70–90 additions: imports, JSX wiring, `handleTeamFieldChange`/`handleLogoChange`; ~180–210 deletions: inline filter/dialog JSX, dead imports, local type decls) |
| 400-line budget risk | Low vs. 800-line-per-PR budget |
| Chained PRs recommended | Yes (this is the second link) |
| Suggested split | PR1 → PR2 (this) |
| Delivery strategy | chained (user-fixed) |
| Chain strategy | stacked-to-main |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | New types + 3 presentational components + characterization suite, monolith untouched | PR 1 | `npm run test --prefix Club12-WebClient -- TeamsPage.test` | Real RTL run: `npm run test --prefix Club12-WebClient` | Revert PR1 = delete the 8 new files; `develop` unaffected |
| 2 | Rewire `TeamsPage.tsx` to consume the 3 children; delete duplicated JSX | PR 2 | `npm run test --prefix Club12-WebClient -- TeamsPage.test` | Real RTL run: `npm run test --prefix Club12-WebClient` + `npm run build --prefix Club12-WebClient` | Revert PR2 = restore monolithic `TeamsPage.tsx`; children stay unused but present |

## PR1: New Files + Characterization Suite

### Phase 1: Characterization Suite First (baseline, strict TDD)

- [x] 1.1 Create `Club12-WebClient/src/views/team/TeamsPage.test.tsx`: mock `@/modules/team/hook/team.hook` (`vi.fn` for `teams`/`addTeam`/`putTeamById`/`getTeamsByFiltered`/`deleteTeamById`), mock `sweetalert2` → `{ isConfirmed: true }`, wrap in `MemoryRouter`, use `vi.useFakeTimers()`.
- [x] 1.2 Add filter/debounce scenario: typed value reflects immediately; `getTeamsByFiltered` called only after 1000ms; page resets to 0 (Req: Filtering).
- [x] 1.3 Add pagination scenario: page 2 click calls `getTeamsByFiltered` with `pageNumber: 2`; loading state shown mid-fetch (Req: Pagination).
- [x] 1.4 Add create scenarios: empty form on open; submit without logo blocked (warning, no `addTeam` call); successful submit calls `addTeam` with trimmed payload, closes/resets/refetches/success alert (Req: Create Dialog).
- [x] 1.5 Add edit scenarios: dialog prefilled (no logo); successful submit calls `putTeamById` with trimmed payload, closes/refetches/success alert (Req: Edit Dialog).
- [x] 1.6 Add delete scenarios: decline skips `deleteTeamById`; confirm calls it with row id + success alert (Req: Delete Confirmation).
- [x] 1.7 Run `npm run test --prefix Club12-WebClient -- TeamsPage.test` against the CURRENT monolithic `TeamsPage.tsx`; all pass (Req: Acceptance Evidence — monolith baseline). `TeamsPage.tsx` stays unmodified.

### Phase 2: Shared Types

- [x] 2.1 Create `Club12-WebClient/src/views/team/teams.types.ts` exporting `TeamsSearchFilters` and `TeamFormState` (verbatim from monolith).

### Phase 3: Presentational Components (RED test → GREEN component, per component)

- [x] 3.1 RED: Create `TeamsFilterBar.test.tsx` against not-yet-created `TeamsFilterBar` (renders filters, fires `onFilterChange`).
- [x] 3.2 GREEN: Create `TeamsFilterBar.tsx` (`TeamsFilterBarProps`) with the 3 `TextField`s moved verbatim.
- [x] 3.3 RED: Create `TeamsTable.test.tsx` against not-yet-created `TeamsTable` (rows/columns render, `loading` passthrough, fires `onPaginationModelChange`).
- [x] 3.4 GREEN: Create `TeamsTable.tsx` (`TeamsTableProps`) wrapping `DataGrid` with hardcoded `getRowId`/`autoHeight`/`disableRowSelectionOnClick`/`disableColumnMenu`.
- [x] 3.5 RED: Create `TeamFormDialog.test.tsx` against not-yet-created `TeamFormDialog` (covers `withLogo=true` renders logo picker + `onLogoChange`; `withLogo=false` omits it; `onFieldChange`/`onConfirm`/`onClose` wiring).
- [x] 3.6 GREEN: Create `TeamFormDialog.tsx` (`TeamFormDialogProps`) merging create/edit JSX; emits raw `onFieldChange(field, value)` only — no `.toUpperCase()` inside (stays pure/presentational).

### Phase 4: PR1 Verification

- [x] 4.1 Run full `npm run test --prefix Club12-WebClient`: all new unit tests + characterization suite green.
- [x] 4.2 Run `npm run build --prefix Club12-WebClient`: new files compile, unused by any view.
- [x] 4.3 Diff-check `TeamsPage.tsx` has zero changes vs. `develop` HEAD before opening PR1.

## PR2: Wire Container

### Phase 5: Container Rewire

- [ ] 5.1 In `TeamsPage.tsx`, import types from `teams.types.ts`; delete local `TeamsSearchFilters`/`TeamFormState` declarations.
- [ ] 5.2 Replace inline filter `Stack` JSX with `<TeamsFilterBar filters={filters} onFilterChange={handleFilterChange} />`.
- [ ] 5.3 Replace inline `DataGrid` JSX with `<TeamsTable rows={rows} columns={columns} loading={loading} noRowsMessage={noRowsMessage} paginationModel={paginationModel} onPaginationModelChange={handlePaginationModelChange} pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS} />`.
- [ ] 5.4 Add `handleTeamFieldChange(field, value)` applying the existing `threeLetterCode.toUpperCase()` transform; use it to replace both dialogs' inline `onChange` handlers.
- [ ] 5.5 Add `handleLogoChange(file)` for the create-only logo field.
- [ ] 5.6 Replace the create `Dialog` JSX with `<TeamFormDialog withLogo open={isCreateModalOpen} title="Nuevo equipo" confirmLabel="Crear" form={teamForm} submitting={submitting} onFieldChange={handleTeamFieldChange} onLogoChange={handleLogoChange} onClose={...} onConfirm={handleCreateSubmit} />`.
- [ ] 5.7 Replace the edit `Dialog` JSX with `<TeamFormDialog withLogo={false} open={Boolean(editingTeam)} title="Editar equipo" confirmLabel="Guardar" .../>` (no `onLogoChange` usage).
- [ ] 5.8 Remove now-unused imports (`TextField`, `InputAdornment`, `SearchIcon`, `Dialog`, `DialogTitle`, `DialogContent`, `DialogActions`, `Button` if unreferenced).

### Phase 6: PR2 Verification

- [ ] 6.1 Run `npm run test --prefix Club12-WebClient -- TeamsPage.test`: SAME characterization file, unmodified, passes against the decomposed tree (Req: Acceptance Evidence — decomposed scenario).
- [ ] 6.2 Run full `npm run test --prefix Club12-WebClient`: all suites green.
- [ ] 6.3 Run `npm run build --prefix Club12-WebClient`: compiles clean.
- [ ] 6.4 Diff-check: no other `views/*Page.tsx` file changed.
