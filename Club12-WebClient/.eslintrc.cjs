module.exports = {
 env: { browser: true, es2020: true, node: true },
 extends: [
  'eslint:recommended',
  'plugin:@typescript-eslint/recommended',
  'plugin:react-hooks/recommended',
 ],
 parser: '@typescript-eslint/parser',
 parserOptions: { ecmaVersion: 'latest', sourceType: 'module' },
 plugins: ['react-refresh'],
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
  'react-refresh/only-export-components': 'warn',
 },
}
