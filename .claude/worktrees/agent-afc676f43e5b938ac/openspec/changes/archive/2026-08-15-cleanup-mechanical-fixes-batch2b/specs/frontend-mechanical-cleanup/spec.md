# Frontend Mechanical Cleanup Specification (Batch 2b)

## Purpose

Define behavior-preservation guarantees for three independent, non-visual
refactors in `Club12-WebClient`: extracting hardcoded hex color literals
(`#FD6B00`, `#d33`) to theme-sourced tokens, consolidating the duplicated
`/token-invalido` route literal to one source of truth, and removing dead
empty i18n files. Each is a pure refactor — the system's rendered output,
navigation behavior, and build output MUST be identical before and after.

## Requirements

### Requirement: Primary Token Hex-Equivalence

Every call site currently using the literal `#FD6B00` MUST, after
extraction to a theme-sourced token (e.g. `theme.palette.primary.main`),
resolve to the exact string `#FD6B00` at render/config time. No call site
MAY use MUI `sx` shorthand (`'primary.main'`) where the consuming API
requires a raw hex string (e.g. SweetAlert `confirmButtonColor`).

#### Scenario: Theme primary color literal is unchanged

- GIVEN `theme.ts` defines `palette.primary.main`
- WHEN the theme is loaded
- THEN `theme.palette.primary.main` MUST equal `'#FD6B00'`

#### Scenario: SweetAlert confirmButtonColor resolves to original hex

- GIVEN a component previously called SweetAlert with
  `confirmButtonColor: '#FD6B00'`
- WHEN the same component is refactored to source the color from the theme
  token
- THEN the resolved `confirmButtonColor` value passed to SweetAlert MUST
  equal `'#FD6B00'` (string equality, not just "references a token")

#### Scenario: Rendered element computed color is unchanged

- GIVEN a component previously styled with the literal `#FD6B00` (e.g. `sx`,
  inline `style`, or CSS-in-JS)
- WHEN it is refactored to reference the theme token
- THEN the element's rendered/computed color value MUST equal `#FD6B00`
  (`rgb(253, 107, 0)`), matching the pre-refactor computed style exactly

### Requirement: Dedicated Cancel/Danger Token Hex-Equivalence

The system MUST expose a NEW, dedicated named token for `#d33` (e.g. a
theme palette augmentation or exported constant) that is distinct from MUI's
default `error.main` (`#d32f2f`). Every SweetAlert `cancelButtonColor` site
currently using the literal `#d33` MUST resolve to the exact string `#d33`
after migrating to this token. `error.main` MUST remain `#d32f2f`
(unmodified) after the change.

#### Scenario: New cancel token resolves to original hex

- GIVEN a component previously called SweetAlert with
  `cancelButtonColor: '#d33'`
- WHEN the same component is refactored to source the color from the new
  dedicated token
- THEN the resolved `cancelButtonColor` value passed to SweetAlert MUST
  equal `'#d33'`

#### Scenario: Default MUI error color is untouched

- GIVEN the new dedicated `#d33` token exists
- WHEN `theme.palette.error.main` is read anywhere else in the app (e.g. a
  component NOT using the cancel-color token)
- THEN it MUST still equal MUI's default `'#d32f2f'`, unchanged by the
  addition of the new token

### Requirement: Single Source of Truth for Invalid-Token Route

The invalid-token redirect path MUST be defined in exactly one place
(`constants/routes.ts`), and every consumer (`axiosUtils.ts`, `App.tsx`)
MUST resolve to that same value rather than each declaring its own literal.

#### Scenario: axiosUtils and App.tsx resolve to the same path value

- GIVEN `routes.ts` defines a `tokenInvalido` entry with value
  `/token-invalido`
- WHEN `axiosUtils.ts`'s redirect logic reads the invalid-token path AND
  `App.tsx`'s route-matching logic reads the invalid-token path
- THEN both MUST resolve to the exact same string, `/token-invalido`,
  sourced from `routes.ts` (not from independently duplicated literals)

#### Scenario: 401 redirect still lands on the invalid-token page

- GIVEN a request fails with a 401 status and an Authorization header was
  present
- WHEN `axiosUtils.ts` triggers the invalid-token redirect
- THEN `window.location.assign` MUST be called with `/token-invalido`,
  identical to pre-refactor behavior

### Requirement: Safe Removal of Dead I18n Files

`modules/core/languajes/spanish.ts` and `english.ts` MUST be deleted only
after confirming zero references exist anywhere in `src`, and the deletion
MUST NOT introduce build, typecheck, or dangling-import failures.

#### Scenario: Zero references confirmed before deletion

- GIVEN a full-source grep for `languajes/spanish` and `languajes/english`
  (import specifiers)
- WHEN the grep is run across `Club12-WebClient/src` prior to deletion
- THEN it MUST return zero matches

#### Scenario: Build and typecheck stay clean after deletion

- GIVEN `spanish.ts` and `english.ts` have been deleted
- WHEN `npm run build` and the project's typecheck command are run
- THEN both MUST succeed with no errors referencing the deleted files or
  any dangling import

## Non-Goals

- No new theme colors beyond the primary token reference and the one new
  dedicated `#d33` token.
- No i18n feature implementation (translation wiring is out of scope).
- No SweetAlert behavior changes beyond sourcing button colors from theme
  tokens.
