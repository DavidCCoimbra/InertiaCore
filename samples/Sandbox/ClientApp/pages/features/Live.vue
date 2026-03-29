<script setup lang="ts">
import { router, useHttp } from '@inertiajs/vue3'
import Navbar from '../../components/Navbar.vue'

defineProps<{
    counter: number
    timestamp: string
    feed: string[]
    serverStatus: string
    currentBid: number
    bidCount: number
}>()

const http = useHttp()

// Actions trigger server-side logic via HTTP, data comes back via WebSocket PushProps
function increment() {
    http.post('/features/live/increment', {}, { async: true })
}

function simulateEvent() {
    http.post('/features/live/simulate-event', {}, { async: true })
}

function refreshViaHttp() {
    router.reload({ only: ['timestamp', 'serverStatus'] })
}

// No polling or SignalR setup needed in this component!
// - [InertiaTimed] → initInertiaCore() in app.ts auto-polls timestamp
// - [InertiaLive] → SignalR plugin auto-subscribes to channels
// - PushProps → data arrives directly over WebSocket
</script>

<template>
    <div class="min-h-screen bg-gray-50">
        <Navbar />
        <main class="max-w-4xl mx-auto px-6 py-8">
            <h1 class="text-2xl font-bold text-gray-900 mb-6">Live Props</h1>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <!-- Counter: direct action → PushProps over WS -->
                <div class="bg-white rounded-lg p-6 shadow-sm border border-indigo-200">
                    <div class="flex items-center justify-between mb-2">
                        <span class="text-xs font-mono text-indigo-600 uppercase">PushProps (direct action)</span>
                        <span class="text-xs text-gray-400">via WebSocket</span>
                    </div>
                    <p class="text-sm text-gray-500">User clicks button → server pushes new value over WS to all tabs</p>
                    <p class="mt-4 text-6xl font-bold text-indigo-600 text-center">{{ counter }}</p>
                    <button @click="increment" class="mt-4 w-full bg-indigo-600 text-white rounded-lg px-4 py-2 hover:bg-indigo-700 transition">
                        Increment
                    </button>
                </div>

                <!-- Timestamp: server-declared polling -->
                <div class="bg-white rounded-lg p-6 shadow-sm border border-teal-200">
                    <div class="flex items-center justify-between mb-2">
                        <span class="text-xs font-mono text-teal-600 uppercase">[InertiaTimed(10s)]</span>
                        <span class="text-xs text-gray-400">auto-poll</span>
                    </div>
                    <p class="text-sm text-gray-500">Server declares interval, client auto-polls. Zero client config.</p>
                    <p class="mt-4 text-4xl font-bold text-teal-600 text-center">{{ timestamp }}</p>
                    <p class="mt-2 text-xs text-gray-400 text-center">Status: {{ serverStatus }}</p>
                </div>

                <!-- Feed: indirect server event → PushProps over WS -->
                <div class="bg-white rounded-lg p-6 shadow-sm border border-purple-200 md:col-span-2">
                    <div class="flex items-center justify-between mb-2">
                        <span class="text-xs font-mono text-purple-600 uppercase">PushProps (indirect event)</span>
                        <span class="text-xs text-gray-400">via WebSocket — no user action</span>
                    </div>
                    <p class="text-sm text-gray-500 mb-4">
                        Simulates an external system (webhook, background job, cron) pushing data to the page.
                        The user doesn't trigger this — the server decides when to push.
                    </p>
                    <div class="bg-gray-50 rounded-lg p-4 max-h-48 overflow-y-auto mb-4">
                        <ul class="text-xs font-mono text-gray-600 space-y-1">
                            <li v-for="(msg, i) in feed" :key="i">{{ msg }}</li>
                        </ul>
                    </div>
                    <div class="flex gap-3">
                        <button @click="simulateEvent" class="bg-purple-600 text-white rounded-lg px-4 py-2 text-sm hover:bg-purple-700 transition">
                            Simulate External Event
                        </button>
                        <button @click="refreshViaHttp" class="bg-gray-200 text-gray-700 rounded-lg px-4 py-2 text-sm hover:bg-gray-300 transition">
                            RefreshProps (HTTP fetch)
                        </button>
                    </div>
                    <p class="mt-3 text-xs text-gray-400">
                        "Simulate External Event" calls a server endpoint that pushes new feed data via PushProps (WebSocket).
                        "RefreshProps" tells the client to re-fetch via HTTP — compare the Network tab.
                    </p>
                </div>

                <!-- Cross-page channel: bid from /features/listing updates here too -->
                <div class="bg-white rounded-lg p-6 shadow-sm border border-emerald-200 md:col-span-2">
                    <div class="flex items-center justify-between mb-2">
                        <span class="text-xs font-mono text-emerald-600 uppercase">PushToChannel (cross-page)</span>
                        <span class="text-xs text-gray-400">channel: listing:mustang</span>
                    </div>
                    <p class="text-sm text-gray-500 mb-4">
                        This bid data updates from <a href="/features/listing" class="text-emerald-600 underline">/features/listing</a>.
                        Both pages subscribe to the same channel. Bid there → updates here.
                    </p>
                    <div class="flex items-center gap-8">
                        <div>
                            <p class="text-sm text-gray-500">Current Bid</p>
                            <p class="text-3xl font-bold text-emerald-600">${{ currentBid?.toLocaleString() }}</p>
                        </div>
                        <div>
                            <p class="text-sm text-gray-500">Bids</p>
                            <p class="text-3xl font-bold text-gray-900">{{ bidCount }}</p>
                        </div>
                    </div>
                </div>
            </div>

            <div class="mt-8 bg-white rounded-lg p-6 shadow-sm border">
                <h2 class="text-lg font-semibold text-gray-900 mb-3">What's happening</h2>
                <ul class="text-sm text-gray-600 space-y-2">
                    <li><strong>Counter (PushProps)</strong> — Click increment → POST to server → server calls <code>broadcaster.PushProps()</code> → value sent over WebSocket → all tabs update instantly. No HTTP roundtrip for other tabs.</li>
                    <li><strong>Timestamp ([InertiaTimed])</strong> — Server declares <code>[InertiaTimed(IntervalSeconds = 10)]</code> on the prop. <code>initInertiaCore()</code> reads the metadata and calls <code>router.reload({{ '{' }} only: ['timestamp'] {{ '}' }})</code> every 10s.</li>
                    <li><strong>Feed (PushProps indirect)</strong> — "Simulate External Event" hits an API endpoint that doesn't redirect — it just pushes data to all connected clients via <code>broadcaster.PushProps()</code>. Simulates a webhook or background job.</li>
                    <li><strong>RefreshProps</strong> — Tells clients "go fetch fresh data via HTTP." Compare with PushProps in the Network tab: RefreshProps causes an Inertia XHR request, PushProps doesn't.</li>
                </ul>
            </div>
        </main>
    </div>
</template>
