/**
 * Re-exports `@g-loot/react-tournament-brackets`'s TypeScript types. The
 * package's public entry point only re-exports runtime values
 * (components, `MATCH_STATES`, `createTheme`) — its type declarations
 * live in `dist/esm/types.d.ts` and aren't re-exported from there, so
 * every consumer in this app imports them from here instead of reaching
 * into the package's internal path directly (see also
 * `src/g-loot-react-tournament-brackets.d.ts` for the matching fix to the
 * package's own broken `types` field).
 */
export type {
  MatchType as LibraryMatch,
  ParticipantType as LibraryParticipant,
  MatchComponentProps as LibraryMatchComponentProps,
  OptionsType as LibraryBracketOptions,
  ThemeType as LibraryBracketTheme,
} from '@g-loot/react-tournament-brackets/dist/esm/types';
