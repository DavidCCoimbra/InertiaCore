<script setup lang="ts">
import { usePage, router } from '@inertiajs/vue3'
import Layout from '../../Components/Layout.vue'
import PropCard from '../../Components/PropCard.vue'

const page = usePage()

function submitForm() {
    router.post('/flash')
}
</script>

<template>
    <Layout>
        <h1 class="text-3xl font-bold mb-2">Flash Data</h1>
        <p class="text-gray-500 mb-6">
            Demonstrates <code class="bg-gray-100 px-1 rounded">Flash()</code> — data that persists through one redirect then auto-clears.
        </p>
        {{page}}

        <div v-if="page.props.flash" class="mb-6">
            <PropCard title="Flash Messages" type="TempData (read-once)">
                <div
                    v-if="(page.props.flash as Record<string, string>)?.success"
                    class="bg-green-50 border border-green-200 rounded p-3 mb-2"
                >
                    <p class="text-green-700 font-semibold">{{ page.props.flash.success }}</p>
                </div>
                <div
                    v-if="(page.props.flash as Record<string, string>)?.timestamp"
                    class="text-sm text-gray-500"
                >
                    Server time: {{ page.props.flash.timestamp }}
                </div>
                <p class="text-sm text-gray-400 mt-2">
                    Refresh the page — flash data will be gone (consumed on read).
                </p>
            </PropCard>
        </div>

        <div v-else class="mb-6">
            <PropCard title="Flash Messages" type="TempData (read-once)">
                <p class="text-gray-400">No flash data. Submit the form to see it in action.</p>
            </PropCard>
        </div>

        <button
            @click="submitForm"
            class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
        >
            Submit Form (POST → redirect → flash)
        </button>
    </Layout>
</template>
