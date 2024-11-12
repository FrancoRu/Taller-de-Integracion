// vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
 plugins: [react()],
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
  port: 3000,
 },
 resolve: {
  alias: {
   '@': path.resolve(__dirname, 'src'), // Ajusta la ruta según la estructura de tu proyecto
  },
 },
})
