# Design: TeamsPage Decomposition (container/presentational split)

## Technical Approach

Split `Club12-WebClient/src/views/team/TeamsPage.tsx` (602 lines) into a stateful
container plus stateless presentational siblings, **zero behavior/visual change**.
The container keeps its default export and `TeamsScreenProps` (verbatim), so
`App.tsx`/`TournamentPage.tsx` imports are untouched. It retains all state,
`useTeam()`, the debounce/fetch effects, every handler, and the `columns`/
`teamActions` memos (they close over handlers + `TeamLogo`/`buildActionsColumn`).
Children are pure props-in. JSX and `sx` move verbatim.

The FE harness (Vitest + @testing-library/react, jsdom) is **already configured**
(`openspec/config.yaml`, 16 existing `*.test.tsx`, `src/test/setup.ts`). The prior
proposal's "bootstrap Vitest" HIGH risk is stale — **no harness task exists**.

## Architecture Decisions

### Types location — dedicated `teams.types.ts`
| Option | Tradeoff | Decision |
|--------|----------|----------|
| Flat sibling `teams.types.ts` for shared aliases | One source for container+children, no circular imports | **Chosen** |
| Co-locate each type in its component | Container needs them too → re-import cycle | Rejected |

`teams.types.ts` exports **types only** (`TeamsSearchFilters`, `TeamFormState`).
Value seeds `EMPTY_FILTERS`/`INITIAL_TEAM_FORM` stay container-internal (state seeds).

### One reusable `TeamFormDialog` via `withLogo`
Create and edit dialogs are identical except create adds a logo file field and
uses different title/confirm label. One dialog with a `withLogo` boolean renders
both. Delete stays an imperative `Swal` handler in the container (not a component).

### Presentational purity — transforms stay with state owner
The dialog is dumb: it emits `onFieldChange(field, rawValue)`. The container's
handler applies the existing `threeLetterCode.toUpperCase()`, preserving behavior
byte-for-byte. `TeamsTable` hardcodes the constant DataGrid flags
(`getRowId`, `autoHeight`, `disableRowSelectionOnClick`, `disableColumnMenu`).

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `views/team/teams.types.ts` | Create (PR1) | `TeamsSearchFilters`, `TeamFormState` |
| `views/team/TeamsFilterBar.tsx` | Create (PR1) | 3 filter `TextField`s |
| `views/team/TeamsTable.tsx` | Create (PR1) | `DataGrid` wrapper |
| `views/team/TeamFormDialog.tsx` | Create (PR1) | Reusable create/edit dialog |
| `views/team/TeamsPage.test.tsx` | Create (PR1) | Characterization suite (monolith) |
| `views/team/*.test.tsx` (×3) | Create (PR1) | Per-component unit tests |
| `views/team/TeamsPage.tsx` | Modify (PR2) | Reduce to container; export unchanged |

## Interfaces / Contracts

```ts
// teams.types.ts
type TeamsSearchFilters = Pick<TeamFiltered, 'name'|'threeLetterCode'|'shirtColor'>;
type TeamFormState = { name: string; threeLetterCode: string; shirtColor: string; logo: File | null };

// TeamsFilterBar
interface TeamsFilterBarProps {
  filters: TeamsSearchFilters;
  onFilterChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => void;
}

// TeamsTable
interface TeamsTableProps {
  rows: ITeamResponse[];
  columns: GridColDef<ITeamResponse>[];
  loading: boolean;
  noRowsMessage: string;
  paginationModel: GridPaginationModel;
  onPaginationModelChange: (m: GridPaginationModel) => void;
  pageSizeOptions: number[];
}

// TeamFormDialog — withLogo maps create vs edit
interface TeamFormDialogProps {
  open: boolean;
  title: string;          // create: "Nuevo equipo" | edit: "Editar equipo"
  confirmLabel: string;   // create: "Crear"        | edit: "Guardar"
  withLogo: boolean;      // create: true (renders logo file Button) | edit: false (omits it)
  form: TeamFormState;
  submitting: boolean;
  onFieldChange: (field: 'name'|'threeLetterCode'|'shirtColor', value: string) => void;
  onLogoChange: (file: File | null) => void; // invoked only when withLogo
  onClose: () => void;
  onConfirm: () => void;
}
```
Create → `<TeamFormDialog withLogo open={isCreateModalOpen} onConfirm={handleCreateSubmit} …/>`.
Edit → `<TeamFormDialog withLogo={false} open={Boolean(editingTeam)} onConfirm={handleEditSubmit} …/>`.

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Characterization (FE) | filter+debounce, pagination reset/refetch, create/edit/delete, validation, no-rows | RTL over `TeamsPage` **default export** |
| Unit (FE) | each child renders/emits props | RTL per component |

**Characterization suite (`TeamsPage.test.tsx`)** — the invariant that proves PR2
preserved behavior. It renders the default export and mocks only external
boundaries: `vi.mock('@/modules/team/hook/team.hook')` returns controlled
`teams`/`addTeam`/`putTeamById`/`getTeamsByFiltered`/`deleteTeamById` (`vi.fn`);
`vi.mock('sweetalert2')` resolves `{ isConfirmed: true }`; wrap in `MemoryRouter`.
Uses `vi.useFakeTimers()` to advance the 1000ms debounce. Asserts user-visible
behavior via RTL queries/interactions (type filter → `getTeamsByFiltered` after 1s;
pagination click → refetch + page-0 reset; create/edit fill+submit → hook called
with payload; delete → `Swal` confirm + `deleteTeamById`). Because it targets the
public export and mocks only boundaries, it is **byte-identical and green in both
PR1 (monolith) and PR2 (decomposed) without modification**.

## Delivery (chained, same branch)

- **PR1** — create `teams.types.ts` + 3 components + their unit tests + the
  characterization suite. `TeamsPage.tsx` is **not modified**; new files are not
  imported by it. `develop` stays fully green: app behaves identically, monolith
  suite passes, `npm run build` compiles the standalone files.
- **PR2** — rewrite container to consume the children, delete duplicated JSX. Same
  characterization suite passes unchanged over the decomposed tree.

## Threat Matrix
N/A — no routing, shell, subprocess, VCS/PR automation, executable-file
classification, or process-integration boundary.

## Migration / Rollout
No migration. Revert PR2 restores the monolith; revert PR1 removes new files.
No data/schema/API impact; `useTeam()` untouched.

## Open Questions
- [ ] None blocking.
