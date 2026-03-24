<script setup lang="ts">
import { router } from '@inertiajs/vue3'
import Layout from '../../Components/Layout.vue'
import PropCard from '../../Components/PropCard.vue'

defineProps<{
    appendList: string[]
    deepConfig: Record<string, unknown>
}>()

function reloadMerge() {
    router.reload({ only: ['appendList', 'deepConfig'] })
}
</script>

<template>
    <Layout>
        <h1 class="text-3xl font-bold mb-2">Merge Strategies</h1>
        <p class="text-gray-500 mb-6">Demonstrates <code class="bg-gray-100 px-1 rounded">MergeProp</code> with different strategies.</p>

        <PropCard title="Append List" type="MergeProp (default: append)">
            <pre class="bg-gray-50 p-2 rounded text-sm">{{ JSON.stringify(appendList, null, 2) }}</pre>
            <p class="text-sm text-gray-400 mt-2">On partial reload, new items append to existing.</p>
        </PropCard>

        <PropCard title="Deep Config" type="MergeProp (deep merge)">
            <pre class="bg-gray-50 p-2 rounded text-sm">{{ JSON.stringify(deepConfig, null, 2) }}</pre>
            <p class="text-sm text-gray-400 mt-2">On partial reload, deeply merges with existing object.</p>
        </PropCard>

        <button @click="reloadMerge" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
            Reload Merge Props
        </button>
    </Layout>
</template>
