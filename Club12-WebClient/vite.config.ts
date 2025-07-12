// vite.config.ts
import { defineConfig, loadEnv } from 'vite';
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
  };
});
