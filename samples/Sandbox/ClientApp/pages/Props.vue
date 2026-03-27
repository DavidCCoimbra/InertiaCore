<script setup lang="ts">
defineProps<{
    always: string
    deferred?: string
    merged?: string[]
    appName: string
    year: number
}>()
</script>

<template>
    <div class="min-h-screen bg-gray-50 p-8">
        <div class="max-w-lg mx-auto">
            <h1 class="text-3xl font-bold text-gray-900 mb-8">Prop Types</h1>

            <div class="space-y-4">
                <div class="bg-white rounded-lg p-4 shadow-sm border">
                    <span class="text-xs font-mono text-indigo-600 uppercase">Always</span>
                    <p class="mt-1 text-gray-900">{{ always }}</p>
                </div>

                <div class="bg-white rounded-lg p-4 shadow-sm border">
                    <span class="text-xs font-mono text-amber-600 uppercase">Deferred</span>
                    <p class="mt-1 text-gray-900">{{ deferred ?? '⏳ loading...' }}</p>
                </div>

                <div class="bg-white rounded-lg p-4 shadow-sm border">
                    <span class="text-xs font-mono text-emerald-600 uppercase">Merged</span>
                    <p class="mt-1 text-gray-900">{{ merged?.join(', ') ?? '⏳ loading...' }}</p>
                </div>
            </div>

            <a href="/" class="mt-8 inline-block text-sm text-indigo-600 hover:text-indigo-800">
                ← Back home
            </a>

            <footer class="mt-8 text-center text-xs text-gray-400">
                {{ appName }} &copy; {{ year }}
            </footer>
        </div>
    </div>
</template>
