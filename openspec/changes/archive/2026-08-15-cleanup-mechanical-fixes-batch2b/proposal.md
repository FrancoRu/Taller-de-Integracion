# Proposal: Cleanup — Mechanical / Behavior-Preserving Fixes (Batch 2b, Frontend)

## Intent

Sibling to archived `cleanup-mechanical-fixes-batch2` (frontend query-keys), which
deferred this scope because combined frontend work exceeded budget. Step (ii) of the
clean-architecture audit: behavior-preserving frontend cleanup in `Club12-WebClient`,
now that the Vitest harness exists (Strict TDD unblocked). No functional or visual change.

**Sizing (recounted, not the stale audit numbers):** `#FD6B00` = 64 occ / 23 files
(1 is the theme source-of-truth, stays); `#d33` = 7 occ / 7 files (all SweetAlert
`cancelButtonColor`). Estimated ~230–280 changed units total (colors dominate). Fits the
800-line single-PR budget comfortably — no further split needed.

## Scope

### In Scope (`Club12-WebClient`)
- Replace ~63 hardcoded `#FD6B00` call-sites with a single theme reference. Note: many are
  SweetAlert `confirmButtonColor` raw strings (not MUI `sx`), so they need the resolved hex
  (`theme.palette.primary.main`), not the `'primary.main'` shorthand.
- Replace 7 `#d33` `cancelButtonColor` sites via a NEW dedicated theme token/constant
  (`#d33` ≠ MUI default `error.main` `#d32f2f`; must not clobber MUI's default error color).
- `axiosUtils.ts:11` `INVALID_TOKEN_PATH = '/token-invalido'` → source from `routes.ts`;
  ADD a `tokenInvalido: '/token-invalido'` entry to `routes.ts` (new client-route-path kind).
- Delete dead empty files `languajes/spanish.ts` and `languajes/english.ts` (empty, zero imports).
- Add a targeted render/computed-style test asserting the extracted primary token renders the
  same color as the prior `#FD6B00` literal.

### Out of Scope (Non-Goals)
- `sendGet` 401-pipeline bug (real fix, own test), `TeamsPage.tsx` decomposition,
  `FormData` field-name/DTO contract, 400-vs-404 backend fix. All deferred to behavior-changing slices.
- Wiring i18n (feature work, not mechanical cleanup).

## Capabilities

### New Capabilities
- None (pure refactor).

### Modified Capabilities
- None (no spec-level behavior change).

## Approach

Add the source-of-truth token to `theme.ts` (primary already exists; add a dedicated
cancel/danger token for `#d33` via a named export or palette augmentation — NOT by overriding
`error.main`, which would shift every default error-state component). Swap call-sites to
reference the token; SweetAlert configs use the resolved hex. `routes.ts` gains one entry;
optionally also de-duplicate the same literal in `App.tsx:67` for consistency. Existing Vitest
suite stays green; one added equivalence test proves visual parity.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/theme.ts` | Modified | Add cancel/danger token; primary is the color source |
| `src/views/**/*.tsx` (23 files) | Modified | `#FD6B00`/`#d33` → theme reference |
| `src/modules/core/constants/routes.ts` | Modified | Add `tokenInvalido` entry |
| `src/modules/core/utils/axiosUtils.ts` | Modified | Source path from `routes.ts` |
| `src/App.tsx` | Modified (optional) | De-dup `/token-invalido` literal |
| `src/modules/core/languajes/*.ts` | Removed | Delete 2 empty dead files |
| test file | New | Color-equivalence render test |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Cancel token overrides MUI default `error.main`, shifting other error UI | Med | Use a dedicated token, do not redefine `error.main` |
| SweetAlert receives a token string instead of hex → wrong render | Med | Use resolved `theme.palette.*`, not `'primary.main'` shorthand |
| Missed `#FD6B00`/`#d33` occurrence | Low | Grep recount (64/7); post-edit grep must return 0 outside `theme.ts` |
| Deleting i18n files that are secretly wired | Low | Confirmed empty + zero imports across `src` |

## Rollback Plan

All edits are localized and reversible via `git revert` of the single PR. No data, schema,
or runtime-config change; deleted files were empty, restorable from history.

## Dependencies

- Vitest harness from the merged test-scaffolding work (already delivered).

## Success Criteria

- [ ] `npm run test` green (existing + new color-equivalence test).
- [ ] Grep for `#FD6B00`/`#d33` returns 0 hits outside `theme.ts`.
- [ ] `INVALID_TOKEN_PATH` sourced from `routes.ts`; redirect still lands on `/token-invalido`.
- [ ] Dead i18n files removed; build/tests unaffected.
- [ ] PR under 800 changed lines.

## Proposal question round (assumptions needing user review)

As a delegated executor I cannot ask interactively; surfacing key assumptions:
1. **Cancel-color token mechanism** = a dedicated named token/exported constant (NOT overriding
   MUI `error.main`), to preserve `#d33` exactly without shifting default error UI. Confirm.
2. **`routes.ts` entry kind** = adding a client-route path (`/token-invalido`, leading slash)
   alongside the existing API-resource names. Optionally also de-dup `App.tsx:67`. Confirm both.
3. **Single PR** accepted (~230–280 units, fits 800) — no further split. Confirm.
