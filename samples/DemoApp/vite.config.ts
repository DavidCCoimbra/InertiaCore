import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import dotnetVite from 'aspnetcore-vite-plugin'

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
