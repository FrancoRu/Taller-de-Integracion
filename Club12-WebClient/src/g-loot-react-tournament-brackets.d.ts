/**
 * `@g-loot/react-tournament-brackets@1.0.31-rc`'s `package.json` declares
 * `"types": "dist/index.d.ts"`, but that file doesn't exist in the
 * published package (only `dist/esm/index.d.ts` and `dist/cjs/index.d.ts`
 * do) — a packaging bug in this pre-release build. This ambient
 * declaration re-points the bare specifier at the real declaration file so
 * the rest of the app can `import ... from '@g-loot/react-tournament-brackets'`
 * normally. Safe to delete once a release fixes the `types` field upstream.
 */
declare module '@g-loot/react-tournament-brackets' {
  export * from '@g-loot/react-tournament-brackets/dist/esm/index';
}
