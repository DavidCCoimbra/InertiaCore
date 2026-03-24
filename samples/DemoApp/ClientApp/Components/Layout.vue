<script setup lang="ts">
import { Link } from '@inertiajs/vue3'
</script>

<template>
    <div class="max-w-3xl mx-auto px-6 py-8 font-sans">
        <nav class="flex gap-4 mb-8 pb-4 border-b border-gray-200">
            <Link href="/" class="text-blue-600 hover:text-blue-800 hover:underline">Home</Link>
            <Link href="/dashboard" class="text-blue-600 hover:text-blue-800 hover:underline">Dashboard</Link>
            <Link href="/shared" class="text-blue-600 hover:text-blue-800 hover:underline">Shared Props</Link>
            <Link href="/merge" class="text-blue-600 hover:text-blue-800 hover:underline">Merge</Link>
        </nav>

        <slot />
    </div>
</template>
