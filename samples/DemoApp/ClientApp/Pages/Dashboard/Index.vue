<script setup lang="ts">
import { router } from '@inertiajs/vue3'
import Layout from '../../Components/Layout.vue'
import PropCard from '../../Components/PropCard.vue'

defineProps<{
    user: string
    stats?: string
    items: number[]
    lazy?: string
    permissions?: string[]
}>()

function loadDeferred() {
    router.reload({ only: ['stats'] })
}

function loadOptional() {
    router.reload({ only: ['lazy'] })
}
</script>

<template>
    <Layout>
        <h1 class="text-3xl font-bold mb-2">Dashboard</h1>
        <p class="text-gray-500 mb-6">All prop types in action.</p>

        <PropCard title="User" type="AlwaysProp">
            <p class="text-lg">{{ user }}</p>
            <p class="text-sm text-gray-400">Always included, even in partial reloads.</p>
        </PropCard>

        <PropCard title="Stats" type="DeferProp">
            <div v-if="stats">
                <p class="text-lg text-green-600">{{ stats }}</p>
            </div>
            <div v-else>
                <p class="text-orange-500 mb-2">Not loaded yet (deferred, group: "analytics")</p>
                <button @click="loadDeferred" class="px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm">
                    Load Stats
                </button>
            </div>
        </PropCard>

        <PropCard title="Items" type="MergeProp">
            <pre class="bg-gray-50 p-2 rounded text-sm">{{ JSON.stringify(items, null, 2) }}</pre>
            <p class="text-sm text-gray-400 mt-2">Merges with client-side data on partial reloads.</p>
        </PropCard>

        <PropCard title="Lazy" type="OptionalProp">
            <div v-if="lazy">
                <p class="text-lg text-green-600">{{ lazy }}</p>
            </div>
            <div v-else>
                <p class="text-orange-500 mb-2">Not loaded yet (excluded from initial load)</p>
                <button @click="loadOptional" class="px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm">
                    Load Lazy
                </button>
            </div>
        </PropCard>

        <PropCard title="Permissions" type="OnceProp">
            <div v-if="permissions">
                <pre class="bg-gray-50 p-2 rounded text-sm">{{ JSON.stringify(permissions, null, 2) }}</pre>
                <p class="text-sm text-gray-400 mt-2">Resolved once, excluded on subsequent requests.</p>
            </div>
            <div v-else>
                <p class="text-orange-500">Not loaded (excluded from initial load)</p>
            </div>
        </PropCard>
    </Layout>
</template>
