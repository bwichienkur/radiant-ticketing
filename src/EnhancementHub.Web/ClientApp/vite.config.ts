import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    dedupe: ['react', 'react-dom', 'react-router', 'react-router-dom'],
  },
  build: {
    outDir: resolve(__dirname, '../wwwroot/spa/react'),
    emptyOutDir: true,
    rollupOptions: {
      input: {
        'spa-shell': resolve(__dirname, 'src/entries/spa-shell.tsx'),
      },
      output: {
        entryFileNames: '[name].js',
        chunkFileNames: 'chunks/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash][extname]',
        manualChunks(id) {
          if (!id.includes('node_modules')) {
            return undefined;
          }

          if (
            id.includes('react-router') ||
            id.includes('/react-dom/') ||
            id.includes('/react/')
          ) {
            return 'vendor-react';
          }

          if (id.includes('cytoscape')) {
            return 'vendor-cytoscape';
          }

          if (id.includes('@microsoft/signalr')) {
            return 'vendor-signalr';
          }

          return undefined;
        },
      },
    },
  },
});
