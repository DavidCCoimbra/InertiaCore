import dotnetVite from '@inertiacore/vite'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

export default defineConfig({
    plugins: [
        dotnetVite({
            input: ['ClientApp/app.ts'],
            refresh: true,
        }),
        tailwindcss(),
        vue(),
    ],
})
