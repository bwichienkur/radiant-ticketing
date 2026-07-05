import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: resolve(__dirname, '../wwwroot/spa/react'),
    emptyOutDir: true,
    rollupOptions: {
      input: {
        'request-detail': resolve(__dirname, 'src/entries/request-detail.tsx'),
        'system-map': resolve(__dirname, 'src/entries/system-map.tsx'),
      },
      output: {
        entryFileNames: '[name].js',
        chunkFileNames: 'chunks/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash][extname]',
      },
    },
  },
});
