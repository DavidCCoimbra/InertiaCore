<script setup lang="ts">
import { usePage, router } from '@inertiajs/vue3'
import Layout from '../../Components/Layout.vue'
import PropCard from '../../Components/PropCard.vue'

const page = usePage()

function submitForm() {
    router.post('/validation')
}
</script>

<template>
    <Layout>
        <h1 class="text-3xl font-bold mb-2">Validation Errors</h1>
        <p class="text-gray-500 mb-6">
            Demonstrates validation errors shared as <code class="bg-gray-100 px-1 rounded">page.props.errors</code>.
        </p>

        <PropCard title="Errors" type="AlwaysProp (shared)">
            <div v-if="page.props.errors && Object.keys(page.props.errors as Record<string, string>).length > 0">
                <ul class="list-disc pl-5 space-y-1">
                    <li
                        v-for="(message, field) in (page.props.errors as Record<string, string>)"
                        :key="field"
                        class="text-red-600"
                    >
                        <strong>{{ field }}:</strong> {{ message }}
                    </li>
                </ul>
                <p class="text-sm text-gray-400 mt-3">
                    Refresh the page — errors will be gone (consumed from TempData).
                </p>
            </div>
            <div v-else>
                <p class="text-gray-400">No validation errors. Submit the form to trigger them.</p>
            </div>
        </PropCard>

        <button
            @click="submitForm"
            class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
        >
            Submit Invalid Form
        </button>
    </Layout>
</template>
