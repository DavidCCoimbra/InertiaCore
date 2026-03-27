import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import dotnetVite from '@inertiacore/vite'

export default defineConfig({
    plugins: [
        vue(),
        tailwindcss(),
        dotnetVite({
            input: 'ClientApp/app.ts',
            ssr: 'ClientApp/ssr.ts',
            refresh: true,
        }),
    ],
})
