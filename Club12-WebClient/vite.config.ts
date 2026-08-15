// vite.config.ts
import { defineConfig } from 'vitest/config';
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
    },
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
      css: true,
    },
  };
});
