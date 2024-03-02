module.exports = {
  env: {
    browser: true,
    es2021: true
  },
  extends: ["standard-with-typescript", "plugin:react/recommended"],
  plugins: ["react"],
  rules: {
    "react/react-in-jsx-scope": "off",
    "@typescript-eslint/explicit-function-return-type": "off",
    "@typescript-eslint/quotes": "off",
    "@typescript-eslint/no-non-null-assertion": "off"
  },
  overrides: [
    {
      env: {
        node: true
      },
      files: [".eslintrc.{js,cjs}"],
      parserOptions: {
        project: "./tsconfig.json",
        ecmaVersion: "latest",
        sourceType: "module"
      }
    }
  ]
}
