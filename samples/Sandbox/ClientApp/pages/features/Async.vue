<script setup lang="ts">
import Navbar from '../../components/Navbar.vue'

defineProps<{
    title: string
    smallProp: string
    heavyPayload?: string[]
}>()
</script>

<template>
    <div class="min-h-screen bg-gray-50">
        <Navbar />
        <main class="max-w-4xl mx-auto px-6 py-8">
            <h1 class="text-2xl font-bold text-gray-900 mb-2">{{ title }}</h1>
            <p class="text-sm text-gray-500 mb-6">{{ smallProp }}</p>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
                <a href="/features/async"
                   class="block bg-white rounded-lg p-4 shadow-sm border hover:border-indigo-300 transition"
                   :class="{ 'border-indigo-500 ring-2 ring-indigo-200': title.includes('Async') }">
                    <h3 class="font-semibold text-gray-900">Async Mode</h3>
                    <p class="text-sm text-gray-500">Props fetched via parallel HTTP. Smaller HTML.</p>
                    <p class="text-xs text-gray-400 mt-1">View source to see minimal &lt;script data-page&gt;</p>
                </a>
                <a href="/features/async-inline"
                   class="block bg-white rounded-lg p-4 shadow-sm border hover:border-indigo-300 transition"
                   :class="{ 'border-indigo-500 ring-2 ring-indigo-200': title.includes('Inline') }">
                    <h3 class="font-semibold text-gray-900">Inline Mode</h3>
                    <p class="text-sm text-gray-500">All props in HTML. Uses [InertiaInlinePageData].</p>
                    <p class="text-xs text-gray-400 mt-1">View source to see full JSON in &lt;script data-page&gt;</p>
                </a>
            </div>

            <div class="bg-white rounded-lg p-6 shadow-sm border">
                <h2 class="text-lg font-semibold text-gray-900 mb-3">Heavy Payload ({{ heavyPayload?.length ?? 0 }} items)</h2>
                <div class="max-h-64 overflow-y-auto">
                    <ul class="text-sm text-gray-600 space-y-1">
                        <li v-for="(item, i) in heavyPayload" :key="i" class="font-mono text-xs">
                            {{ item }}
                        </li>
                        <li v-if="!heavyPayload" class="text-gray-400">Loading payload...</li>
                    </ul>
                </div>
            </div>

            <div class="mt-6 bg-gray-100 rounded-lg p-4 text-xs text-gray-500">
                <p><strong>Tip:</strong> Right-click → View Page Source to compare the &lt;script data-page&gt; size between async and inline modes.</p>
                <p class="mt-1"><strong>[InertiaPageData]</strong> on Title and SmallProp keeps them in the HTML even in async mode.</p>
            </div>
        </main>
    </div>
</template>
