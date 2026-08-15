# Proposal: Cleanup — Mechanical / Behavior-Preserving Fixes (Batch 2, Frontend — Query Keys)

## Intent

Frontend half of step (ii) of the archived clean-architecture audit, sibling to backend
`batch1`. Behavior-preserving cleanup in `Club12-WebClient`, now that the Vitest + Testing
Library harness works (Strict TDD unblocked). No functional, routing, or contract change.

**Split decision (budget-driven, recount):** A Grep recount shows the audit under-stated the
frontend total. Query-key literals are **~120–156 tuples across 12 module contexts** (72
`queryKey:` refs plus `setQueryData`/`removeQueries` inline arrays), not "~13 per module".
Combined frontend scope (query keys + 71 color occurrences + axios/i18n) estimates
**~680–700 changed units with a plausible >800 excursion**. Same batch1 discipline: split
rather than silently risk the 800 budget. **This proposal = query-key factories only**
(~500 units, fits 800 with margin). **`batch2b` (deferred sibling) = colors + axios path +
i18n deletion** (~180 units).

## Scope

### In Scope (`Club12-WebClient`)
- Introduce a per-module query-key factory (e.g. `modules/{module}/queryKeys.ts` exporting
  `xKeys = { all, list, byId, ... }`) for the 12 modules with inline literals.
- Replace every inline query-key literal in `modules/*/context/*.context.tsx`
  (`useQuery`/`fetchQuery` `queryKey`, `invalidateQueries`, `setQueryData`, `removeQueries`)
  with the factory. Emitted key tuples MUST stay byte-identical (cache identity preserved).

### Out of Scope (Non-Goals — deferred)
- **`batch2b` (recommended next):** brand colors `#FD6B00`/`#d33` → `theme.ts` palette (71
  occ / 23 view files; note `#d33` ≠ MUI default `error.main` `#d32f2f`, needs an explicit
  palette entry to stay behavior-preserving); `axiosUtils.ts:11` `INVALID_TOKEN_PATH` →
  `routes.ts` (add a `token-invalido` route entry first); delete dead empty i18n files
  `modules/core/languajes/{spanish,english}.ts`.
- Deferred behavior changes (later slices): `sendGet` 401-pipeline bug, `TeamsPage`
  container/presentational split, `FormData` field-name/DTO contract.

## Capabilities

### New Capabilities
- None (pure refactor; no product behavior introduced).

### Modified Capabilities
- None (no spec-level behavior change).

## Approach

Add a small pure-function factory per module; swap each inline literal for the matching
factory call. Because factory output must equal the prior literal exactly, cache reads/writes
and invalidations are unchanged. Verify against existing smoke tests (`npm run test`); add
narrow unit tests asserting each factory returns the exact prior tuple. Mechanical,
compiler-checked symbol usage — no query-fn, route, or DTO change.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/modules/{12 modules}/queryKeys.ts` | New | Per-module key factory |
| `src/modules/*/context/*.context.tsx` | Modified | Literals → factory calls |
| `src/modules/*/__tests__` | New | Factory equivalence unit tests |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Factory emits a different tuple → cache miss/stale | Med | Equivalence unit tests per factory; keep exact tuple shape |
| A literal missed / duplicated key | Low | Grep sweep + TS compile + smoke tests |
| Slice still exceeds 800 once counted | Low | Query-key-only ~500; colors carved to `batch2b` |

## Rollback Plan

Localized, additive edits (new factory files + call swaps); `git revert` of the single PR
restores prior literals. No data, schema, route, or runtime-config change; no migration.

## Dependencies

- Frontend test harness (Vitest + Testing Library) — already present.
- `batch1` (backend) is independent; no ordering dependency.

## Success Criteria

- [ ] `npm run test` green (existing smoke + new factory equivalence tests).
- [ ] Every inline query-key literal in the 12 module contexts replaced; tuples byte-identical.
- [ ] Zero routing/query-fn/DTO change; no cache-key regression.
- [ ] PR under 800 changed lines; `batch2b` (colors + axios + i18n) filed as sibling.

## Proposal question round (assumptions needing user review)

Surfacing key assumptions (delegated executor, non-interactive):
1. **Split accepted?** batch2 = query-key factories; batch2b = colors + axios path + i18n
   deletion (proposed next). If a single frontend PR is preferred, raise the 800 budget or
   accept a `size:exception` — the recount makes the combined slice tight.
2. **Factory location/shape** = `modules/{module}/queryKeys.ts` exporting a `xKeys` object.
   Confirm this over a single global factory file.
3. **`#d33` handling (batch2b)** = add an explicit palette entry rather than reuse MUI's
   default `error.main` (colors differ), to keep it behavior-preserving. Confirm intent.
