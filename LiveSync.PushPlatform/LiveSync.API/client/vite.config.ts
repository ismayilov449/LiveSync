import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5252',
      '/hubs': { target: 'http://localhost:5252', ws: true },
      '/health': 'http://localhost:5252',
    },
  },
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
});
