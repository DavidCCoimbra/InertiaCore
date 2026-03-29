<script setup lang="ts">
import { useHttp } from '@inertiajs/vue3'
import Navbar from '../../components/Navbar.vue'

defineProps<{
    listingTitle: string
    currentBid: number
    bidCount: number
}>()

const http = useHttp()

function placeBid() {
    // Fire-and-forget: response comes via WebSocket PushToChannel, not HTTP
    http.post('/features/listing/bid', {}, { async: true })
}
</script>

<template>
    <div class="min-h-screen bg-gray-50">
        <Navbar />
        <main class="max-w-4xl mx-auto px-6 py-8">
            <h1 class="text-2xl font-bold text-gray-900 mb-2">{{ listingTitle }}</h1>
            <p class="text-sm text-gray-500 mb-6">Channel-based live prop demo — bid updates across all pages</p>

            <div class="bg-white rounded-lg p-8 shadow-sm border border-emerald-200">
                <div class="text-center">
                    <p class="text-sm text-gray-500 uppercase tracking-wide">Current Bid</p>
                    <p class="text-5xl font-bold text-emerald-600 mt-2">${{ currentBid?.toLocaleString() }}</p>
                    <p class="text-sm text-gray-400 mt-2">{{ bidCount }} bids placed</p>
                </div>

                <button @click="placeBid" class="mt-6 w-full bg-emerald-600 text-white rounded-lg px-4 py-3 text-lg font-semibold hover:bg-emerald-700 transition">
                    Place Bid
                </button>

                <div class="mt-6 bg-emerald-50 rounded-lg p-4 text-sm text-emerald-800">
                    <p class="font-semibold mb-1">Cross-page live update</p>
                    <p>Open <a href="/features/live" class="underline">/features/live</a> in another tab. Both pages subscribe to the <code class="bg-emerald-100 px-1 rounded">listing:mustang</code> channel. Bidding here updates both pages instantly via WebSocket.</p>
                </div>
            </div>

            <div class="mt-6 bg-white rounded-lg p-6 shadow-sm border">
                <h2 class="text-lg font-semibold text-gray-900 mb-3">How it works</h2>
                <pre class="text-xs text-gray-600 bg-gray-50 p-4 rounded-lg overflow-x-auto">// Server: same channel on different pages
// Listing page:
CurrentBid = Inertia.Always(5200).WithLive("listing:mustang")

// Live page (different component, same channel):
CurrentBid = Inertia.Always(5200).WithLive("listing:mustang")

// When someone bids:
await broadcaster.PushToChannel("listing:mustang", new {{ '{' }}
    currentBid = 5500,
    bidCount = 48,
{{ '}' }});
// → Both pages update instantly via WebSocket</pre>
            </div>
        </main>
    </div>
</template>
