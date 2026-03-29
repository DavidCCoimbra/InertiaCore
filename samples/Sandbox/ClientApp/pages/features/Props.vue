<script setup lang="ts">
import Navbar from '../../components/Navbar.vue'

defineProps<{
    serverTime: string
    heavyData?: string
    analytics?: string
    items?: string[]
    permissions?: string[]
    secretData?: string
    isAdmin: boolean
    adminPanel?: string
    stats?: string
    liveClock: string
}>()
</script>

<template>
    <div class="min-h-screen bg-gray-50">
        <Navbar />
        <main class="max-w-4xl mx-auto px-6 py-8">
            <h1 class="text-2xl font-bold text-gray-900 mb-6">Prop Types Showcase</h1>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <!-- Always -->
                <div class="bg-white rounded-lg p-4 shadow-sm border border-indigo-200">
                    <span class="text-xs font-mono text-indigo-600 uppercase">[InertiaAlways]</span>
                    <p class="mt-1 text-sm text-gray-500">Included even during partial reloads</p>
                    <p class="mt-2 text-gray-900 font-medium">{{ serverTime }}</p>
                </div>

                <!-- Defer -->
                <div class="bg-white rounded-lg p-4 shadow-sm border border-amber-200">
                    <span class="text-xs font-mono text-amber-600 uppercase">[InertiaDefer]</span>
                    <p class="mt-1 text-sm text-gray-500">Loaded via deferred partial reload after mount</p>
                    <p class="mt-2 text-gray-900 font-medium">{{ heavyData ?? '⏳ loading...' }}</p>
                </div>

                <!-- Defer (grouped) -->
                <div class="bg-white rounded-lg p-4 shadow-sm border border-amber-200">
                    <span class="text-xs font-mono text-amber-600 uppercase">[InertiaDefer(Group = "analytics")]</span>
                    <p class="mt-1 text-sm text-gray-500">Grouped defers fetched in one request</p>
                    <p class="mt-2 text-gray-900 font-medium">{{ analytics ?? '⏳ loading...' }}</p>
                </div>

                <!-- Merge -->
                <div class="bg-white rounded-lg p-4 shadow-sm border border-emerald-200">
                    <span class="text-xs font-mono text-emerald-600 uppercase">[InertiaMerge]</span>
                    <p class="mt-1 text-sm text-gray-500">Merges with existing client-side data</p>
                    <p class="mt-2 text-gray-900 font-medium">{{ items?.join(', ') ?? '⏳ loading...' }}</p>
                </div>

                <!-- Once -->
                <div class="bg-white rounded-lg p-4 shadow-sm border border-purple-200">
                    <span class="text-xs font-mono text-purple-600 uppercase">[InertiaOnce]</span>
                    <p class="mt-1 text-sm text-gray-500">Resolved once, cached by client</p>
                    <p class="mt-2 text-gray-900 font-medium">{{ permissions?.join(', ') ?? '⏳ loading...' }}</p>
                </div>

                <!-- Optional -->
                <div class="bg-white rounded-lg p-4 shadow-sm border border-sky-200">
                    <span class="text-xs font-mono text-sky-600 uppercase">[InertiaOptional]</span>
                    <p class="mt-1 text-sm text-gray-500">Excluded from initial load, fetch on demand</p>
                    <p class="mt-2 text-gray-900 font-medium">{{ secretData ?? '(not requested)' }}</p>
                </div>

                <!-- Conditional (When) -->
                <div class="bg-white rounded-lg p-4 shadow-sm border border-rose-200">
                    <span class="text-xs font-mono text-rose-600 uppercase">[InertiaWhen]</span>
                    <p class="mt-1 text-sm text-gray-500">Conditionally included (isAdmin={{ isAdmin }})</p>
                    <p class="mt-2 text-gray-900 font-medium">{{ adminPanel ?? '(condition false — prop absent)' }}</p>
                </div>

                <!-- Fallback -->
                <div class="bg-white rounded-lg p-4 shadow-sm border border-orange-200">
                    <span class="text-xs font-mono text-orange-600 uppercase">[InertiaFallback]</span>
                    <p class="mt-1 text-sm text-gray-500">Falls back to FallbackStats if resolution fails</p>
                    <p class="mt-2 text-gray-900 font-medium">{{ stats ?? '⏳ loading fallback...' }}</p>
                </div>

                <!-- Timed -->
                <div class="bg-white rounded-lg p-4 shadow-sm border border-teal-200 md:col-span-2">
                    <span class="text-xs font-mono text-teal-600 uppercase">[InertiaTimed(IntervalSeconds = 5)]</span>
                    <p class="mt-1 text-sm text-gray-500">Server-driven polling every 5 seconds</p>
                    <p class="mt-2 text-gray-900 font-medium text-lg">{{ liveClock }}</p>
                </div>
            </div>
        </main>
    </div>
</template>
