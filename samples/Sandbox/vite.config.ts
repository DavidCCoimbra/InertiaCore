import dotnetVite from '@inertiacore/vite'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

export default defineConfig({
    plugins: [
        dotnetVite({
            input: ['ClientApp/app.ts'],
            ssr: 'ClientApp/ssr.ts',
            refresh: true,
            ssrDev: process.env.SSR_MODE === 'v8'
                ? { v8: true }
                : true,
        }),
        tailwindcss(),
        vue(),
    ],
})
