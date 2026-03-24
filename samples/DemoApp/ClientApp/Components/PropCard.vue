<script setup lang="ts">
defineProps<{
    title: string
    type: string
}>()
</script>

<template>
    <div class="border border-gray-200 rounded-lg p-4 mb-4">
        <h3 class="text-lg font-semibold mb-2">
            {{ title }}
            <span class="text-xs text-gray-400 font-normal ml-2">{{ type }}</span>
        </h3>
        <slot />
    </div>
</template>
