<script setup lang="ts">
defineProps<{
    title: string
    message: string
}>()
</script>

<template>
    <div class="min-h-screen flex items-center justify-center bg-gray-50">
        <div class="text-center">
            <h1 class="text-4xl font-bold text-gray-900">{{ title }}</h1>
            <p class="mt-4 text-lg text-gray-600">{{ message }}</p>
            <p class="mt-8 text-sm text-gray-400">
                If you can see this, InertiaCore + Vite + Tailwind is working.
            </p>
            <a href="/props" class="mt-6 inline-block px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition">
                Test Props →
            </a>
        </div>
    </div>
</template>
