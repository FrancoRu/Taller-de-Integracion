# Proposal: Statistics Page Filter UX — Show "Todas" and Stop the Full-Page Reload

**Touches**: Frontend only (`Club12-WebClient`, `StatisticsPage.tsx`). No
backend, no API, no schema.

## Intent

`/panel/estadisticas` has two filter-bar defects:

1. **The "Temporada" and "Torneo" selects never show their default option.**
   Both use `value=""` with a `<MenuItem value="">Todas</MenuItem>` /
   `Todos`. MUI treats `''` as "no selection": without `displayEmpty` the
   Select renders nothing and the floating label sits over the field like a
   placeholder. The "all" scope works — it just looks unselected.

2. **Changing any filter reloads the whole page.** The component returns
   early with a full-screen `CardGridSkeleton` whenever `loading` is true,
   and `loading` flips to `true` on every scope change. The `FilterBar` (and
   the open dropdown the user just clicked) unmounts and remounts on each
   pick. Only the statistics content should refresh.

## Scope

### In Scope

- Both selects render their `value=""` option (`displayEmpty`) with the
  label kept shrunk, so "Todas" / "Todos" shows when nothing is picked.
- `StatisticsPage` keeps `PageShell` + `FilterBar` mounted across filter
  changes. The initial load still shows the skeleton (no `summary` yet); a
  refilter keeps the last numbers on screen with a thin `LinearProgress`
  while the new scope loads.

### Out of Scope (Non-Goals)

- Removing the existing `useMemo` / `useRef` memoisation in the file (React
  Compiler makes it redundant, but that is a separate cleanup).
- Debouncing the filter fetches or changing what the filters fetch.
- The goleadores card's own `scorersLoading` skeleton (already correct).
- Any other admin filter bar (`UsersPage`, sanctions, etc.) — `UsersPage`
  already keeps its `FilterBar` outside the loading branch; this change
  brings `StatisticsPage` in line.

## Capabilities

### New Capabilities

- `statistics-filter-ux`: how the statistics page presents its scope filters
  and reloads on a scope change.

### Modified Capabilities

- None.

## Approach

Add `slotProps={{ select: { displayEmpty: true }, inputLabel: { shrink: true } }}`
to both `<TextField select>`. Restructure the render so the single return
always mounts the filter bar and only swaps the content region: the
`CardGridSkeleton` shows while `summary` is null; once there is a `summary`,
the cards stay rendered and a `LinearProgress` marks an in-flight refilter.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Club12-WebClient/src/views/panel/StatisticsPage.tsx` | Modified | `slotProps` on both selects; single return, filter bar always mounted, `LinearProgress` on refilter |
| `Club12-WebClient/src/views/panel/StatisticsPage.test.tsx` | Modified | Selects show "Todas"/"Todos"; filter bar survives a refilter |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Forcing `shrink: true` looks odd when the field truly is empty | Low | It is the documented MUI workaround; the field shows "Todas", not blank |
| Keeping stale numbers visible during a refilter confuses | Low | `LinearProgress` signals the reload; sub-second in practice |
| A test asserted the full-page skeleton on refilter | Low | Verified: `StatisticsPage.test.tsx` only asserts scoping calls and card values |

## Rollback Plan

Revert the commit. Pure presentational frontend change, no data or contract.

## Success Criteria

- [ ] With no filter picked, "Temporada" shows "Todas" and "Torneo" shows "Todos".
- [ ] Picking a season or tournament does not unmount the filter bar; the
      cards update in place with a progress indicator.
- [ ] Frontend suite green; `tsc` + lint clean.
