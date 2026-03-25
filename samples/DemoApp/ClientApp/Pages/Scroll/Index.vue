<script setup lang="ts">
import { router } from '@inertiajs/vue3'
import Layout from '../../Components/Layout.vue'
import PropCard from '../../Components/PropCard.vue'

const props = defineProps<{
    items: {
        data: string[]
        page: string
        prevPage: number | null
        nextPage: number | null
        currentPage: number
    }
    totalPages: number
}>()

function loadPage(page: number) {
    router.visit(`/scroll?page=${page}`, { preserveState: true })
}

function loadNext() {
    if (props.items.nextPage) {
        loadPage(props.items.nextPage)
    }
}

function loadPrev() {
    if (props.items.prevPage) {
        loadPage(props.items.prevPage)
    }
}
</script>

<template>
    <Layout>
        <h1 class="text-3xl font-bold mb-2">Scroll / Pagination</h1>
        <p class="text-gray-500 mb-6">
            Demonstrates <code class="bg-gray-100 px-1 rounded">ScrollProp&lt;T&gt;</code> with pagination metadata.
        </p>

        <PropCard title="Items" type="ScrollProp&lt;string[]&gt;">
            <ul class="space-y-1 mb-4">
                <li
                    v-for="item in items.data"
                    :key="item"
                    class="bg-gray-50 px-3 py-2 rounded text-sm"
                >
                    {{ item }}
                </li>
            </ul>

            <div class="flex items-center gap-4 text-sm">
                <button
                    @click="loadPrev"
                    :disabled="!items.prevPage"
                    class="px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-30 disabled:cursor-not-allowed"
                >
                    Previous
                </button>

                <span class="text-gray-500">
                    Page {{ items.currentPage }} of {{ totalPages }}
                </span>

                <button
                    @click="loadNext"
                    :disabled="!items.nextPage"
                    class="px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-30 disabled:cursor-not-allowed"
                >
                    Next
                </button>
            </div>
        </PropCard>

        <PropCard title="Scroll Metadata" type="IProvidesScrollMetadata">
            <pre class="bg-gray-50 p-2 rounded text-sm">{{ JSON.stringify({
                page: items.page,
                currentPage: items.currentPage,
                prevPage: items.prevPage,
                nextPage: items.nextPage,
            }, null, 2) }}</pre>
        </PropCard>
    </Layout>
</template>
