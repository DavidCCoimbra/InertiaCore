<script setup lang="ts">
import { usePage } from '@inertiajs/vue3'
import Layout from '../../Components/Layout.vue'
import PropCard from '../../Components/PropCard.vue'

defineProps<{
    pageTitle: string
}>()

const page = usePage()
</script>

<template>
    <Layout>
        <h1 class="text-3xl font-bold mb-2">{{ pageTitle }}</h1>
        <p class="text-gray-500 mb-6">Demonstrates <code class="bg-gray-100 px-1 rounded">Share()</code> and <code class="bg-gray-100 px-1 rounded">ShareOnce()</code>.</p>

        <PropCard title="App Name" type="Share()">
            <p class="text-lg">{{ page.props.appName }}</p>
        </PropCard>

        <PropCard title="Timestamp" type="Share()">
            <p class="text-lg font-mono text-sm">{{ page.props.timestamp }}</p>
            <p class="text-sm text-gray-400 mt-1">Changes on every request (shared per-request).</p>
        </PropCard>

        <PropCard title="Server Info" type="ShareOnce()">
            <div v-if="page.props.serverInfo">
                <pre class="bg-gray-50 p-2 rounded text-sm">{{ JSON.stringify(page.props.serverInfo, null, 2) }}</pre>
                <p class="text-sm text-gray-400 mt-2">Resolved once via OnceProp, excluded on subsequent visits.</p>
            </div>
            <div v-else>
                <p class="text-orange-500">Not loaded (OnceProp excluded)</p>
            </div>
        </PropCard>
    </Layout>
</template>
