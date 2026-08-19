import js from '@eslint/js';
import tsPlugin from '@typescript-eslint/eslint-plugin';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import globals from 'globals';

// Flat-config equivalent of the previous .eslintrc.cjs (env: browser/es2020/node,
// eslint:recommended + typescript-eslint recommended + react-hooks recommended,
// same custom no-unused-vars options and react-refresh warning).
//
// Note: eslint-plugin-react-hooks@7's own "recommended" preset now bundles a much
// larger, React-Compiler-oriented rule set (purity, immutability, set-state-in-*,
// etc.). Adopting that wholesale would be a scope change well beyond a dependency
// bump, so only the two rules that were actually active before
// (rules-of-hooks, exhaustive-deps) are enabled here.
export default [
  {
    // Restrict linting to TypeScript sources only (matches the previous
    // `--ext ts,tsx` CLI flag, which flat config no longer supports).
    ignores: ['build/**', 'coverage/**', '**/*.mjs', '**/*.cjs', '**/*.js'],
  },
  {
    files: ['**/*.{ts,tsx}'],
    ...js.configs.recommended,
  },
  ...tsPlugin.configs['flat/recommended'],
  {
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      parserOptions: {
        ecmaVersion: 'latest',
        sourceType: 'module',
      },
      globals: {
        ...globals.browser,
        ...globals.es2020,
        ...globals.node,
      },
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      '@typescript-eslint/no-unused-vars': [
        'warn',
        {
          vars: 'all',
          args: 'after-used',
          ignoreRestSiblings: true,
          argsIgnorePattern: '^_',
        },
      ],
      'react-hooks/rules-of-hooks': 'error',
      'react-hooks/exhaustive-deps': 'warn',
      'react-refresh/only-export-components': 'warn',
    },
  },
  {
    // Context modules intentionally colocate the Context object with its
    // Provider component (existing app-wide pattern); this only affects
    // dev-time Fast Refresh granularity, not runtime behavior.
    files: ['**/*.context.tsx'],
    rules: {
      'react-refresh/only-export-components': 'off',
    },
  },
];
