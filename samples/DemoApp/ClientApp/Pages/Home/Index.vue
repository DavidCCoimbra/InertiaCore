<script setup lang="ts">
import Layout from '../../Components/Layout.vue'
import PropCard from '../../Components/PropCard.vue'

defineProps<{
    greeting: string
}>()
</script>

<template>
    <Layout>
        <h1 class="text-3xl font-bold mb-2">Home</h1>
        <p class="text-gray-500 mb-6">Basic page with anonymous object props.</p>

        <PropCard title="Greeting" type="string">
            <p class="text-lg">{{ greeting }}</p>
        </PropCard>
    </Layout>
</template>
