// vite.config.ts
import { configDefaults, defineConfig } from 'vitest/config';
import { loadEnv } from 'vite';
import react from '@vitejs/plugin-react-swc';
import tsconfigPaths from 'vite-tsconfig-paths';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd());
  return {
    plugins: [react(), tsconfigPaths()],
    build: {
      outDir: 'build',
      assetsDir: 'assets',
      sourcemap: true,
      minify: 'terser',
    },
    optimizeDeps: {
      include: ['react', 'react-dom'],
    },
    server: {
      port: parseInt(env.VITE_PORT) || 5173,
      proxy: {
        '/api': {
          target: `https://localhost:${env.VITE_BACKEND_PORT}`,
          changeOrigin: true,
          secure: false, // accept the .NET dev HTTPS certificate
        },
      },
    },
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
      css: true,
      // e2e/ holds @playwright/test specs (run via `pnpm test:e2e`), which
      // use an incompatible test()/describe() API — without this exclude,
      // vitest's default include glob picks up *.spec.ts too and fails
      // trying to run them as unit tests.
      exclude: [...configDefaults.exclude, 'e2e/**'],
    },
  };
});
