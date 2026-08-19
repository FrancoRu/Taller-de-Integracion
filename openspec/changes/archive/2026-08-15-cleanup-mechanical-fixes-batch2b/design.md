# Design: Cleanup — Mechanical / Behavior-Preserving Fixes (Batch 2b, Frontend)

## Technical Approach

Behavior-preserving refactor in `Club12-WebClient`. Centralize two hardcoded color
literals and one route literal into existing single-source-of-truth modules, then
delete two dead i18n files. No visual, functional, or spec-level change. Verified
against real files: `theme.ts` already declares `primary.main = '#FD6B00'`;
`main.tsx` wires `<ThemeProvider theme={theme}>`; `routes.ts` imports nothing;
i18n files are empty with zero imports (only unrelated "Spanish" comments exist).

## Architecture Decisions

### Decision: One universal color mechanism — import the `theme` object, read `theme.palette.primary.main`

**Choice**: All `#FD6B00` call-sites (SweetAlert raw strings AND MUI `sx`) import the
default `theme` and read `theme.palette.primary.main`.
**Alternatives considered**: (a) MUI `'primary.main'` shorthand for `sx` sites + resolved
hex for SweetAlert — two mechanisms, inconsistent; (b) a duplicated exported `PRIMARY = '#FD6B00'`
constant — reintroduces a second literal that can drift from the palette.
**Rationale**: SweetAlert's `Swal.fire()` config needs a literal string, not React context, so
`'primary.main'` shorthand cannot reach it. Reading `theme.palette.primary.main` from the imported
object returns the identical resolved hex the ThemeProvider uses — one mechanism everywhere, single
source, zero drift. For `sx` sites (`ErrorPageActions.tsx`, `ErrorPageLayout.tsx`) it is equally
valid and behavior-identical.

### Decision: `#d33` cancel color as a named export in `theme.ts` (NOT a palette key, NOT `error.main`)

**Choice**: Add `export const CANCEL_BUTTON_COLOR = '#d33';` to `theme.ts`. The default `theme`
export and `primary.main` are unchanged.
**Alternatives considered**: (a) override MUI `error.main` — would shift every default error-state
component (`#d32f2f` ≠ `#d33`); (b) augment the MUI palette with a custom key — requires
`declare module '@mui/material/styles'` TS augmentation for a value never consumed through the
theme system (all 7 sites are SweetAlert raw strings); (c) a new `constants/colors.ts` — a second
color-authority file competing with `theme.ts`.
**Rationale**: `theme.ts` is already the color authority; co-locating the token there adds no new
file and no TS ceremony. A plain named export is exactly what SweetAlert's raw-string config needs.

### Decision: `tokenInvalido` route entry sourced by both consumers

**Choice**: Add `tokenInvalido: '/token-invalido'` to `routes.ts`. `axiosUtils.ts:11` becomes
`const INVALID_TOKEN_PATH = routes.tokenInvalido;` (import already present). `App.tsx:67` reads
`routes.tokenInvalido` (de-dup the literal).
**Alternatives considered**: keep the `App.tsx` literal — leaves a second copy that can drift.
**Rationale**: `routes.ts` imports nothing, so no import cycle is possible even though `axiosUtils`
is loaded early/globally. Single source for the redirect target.

## Data Flow

    theme.ts ──(default export)──► main.tsx ThemeProvider (unchanged)
       │  palette.primary.main (#FD6B00)          CANCEL_BUTTON_COLOR (#d33)
       └──────────────► Swal.fire({ confirmButtonColor, cancelButtonColor })
                        └──► MUI sx ({ color, borderColor })

    routes.ts.tokenInvalido ──► axiosUtils redirectToInvalidToken()
                            └─► App.tsx route guard

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/theme.ts` | Modify | Add `export const CANCEL_BUTTON_COLOR = '#d33';`; palette unchanged |
| `src/views/**/*.tsx` (~23 files) | Modify | `#FD6B00` → `theme.palette.primary.main`; `#d33` → `CANCEL_BUTTON_COLOR` (import from `@/theme`) |
| `src/views/core/components/ErrorPageActions.tsx`, `ErrorPageLayout.tsx` | Modify | Replace local `const ORANGE` with `theme.palette.primary.main` |
| `src/modules/core/constants/routes.ts` | Modify | Add `tokenInvalido: '/token-invalido'` |
| `src/modules/core/utils/axiosUtils.ts` | Modify | `INVALID_TOKEN_PATH = routes.tokenInvalido` |
| `src/App.tsx` | Modify | Line 67 uses `routes.tokenInvalido` |
| `src/modules/core/languajes/spanish.ts`, `english.ts` | Delete | Empty, zero imports |
| `src/theme.color-tokens.test.ts` (or similar) | Create | Color-equivalence guard test |

## Interfaces / Contracts

```ts
// theme.ts
export const CANCEL_BUTTON_COLOR = '#d33';
// routes.ts
const routes = { /* ...existing... */ tokenInvalido: '/token-invalido' };
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `theme.palette.primary.main` === `#FD6B00` (case-insensitive) and `CANCEL_BUTTON_COLOR` === `#d33` | Vitest value-equality guard — deterministic, proves parity without flaky jsdom computed-style |
| Unit | `routes.tokenInvalido` === `/token-invalido` and equals path used by `App`/`axiosUtils` | Import assertion |
| Regression | Existing Vitest suite stays green | `npm run test` |

Rationale: a value-equality guard is chosen over a rendered computed-style assertion because MUI
color resolution in jsdom is brittle; equality on the resolved token proves the literal was
preserved exactly.

## Threat Matrix

N/A — no server routing, shell, subprocess, VCS/PR automation, executable-file classification, or
process-integration boundary. `routes.tokenInvalido` is a client-side route-path string constant.

## Migration / Rollout

No migration required. All edits localized and reversible via `git revert` of the single PR;
deleted files were empty and restorable from history.

## Open Questions

- [ ] None blocking. Proposal assumptions (dedicated cancel token, `routes.ts` client-path entry +
      `App.tsx` de-dup, single PR) are all adopted here and verified against source.
