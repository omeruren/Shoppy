import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    // Must stay within the backend's CORS allow-list (see appsettings.Development.json) —
    // Vite's own default port (5173) is not whitelisted there.
    port: 5176,
    strictPort: true,
  },
})
