<script setup lang="ts">
import { computed } from 'vue'
import { usePage, useForm, router } from '@inertiajs/vue3'
import Navbar from '../../components/Navbar.vue'

const page = usePage()
const form = useForm({ message: '' })

const flash = computed(() => page.props.flash as Record<string, string> | undefined)
const errors = computed(() => page.props.errors as Record<string, string> | undefined)
const hasErrors = computed(() => errors.value && Object.keys(errors.value).length > 0)
</script>

<template>
    <div class="min-h-screen bg-gray-50">
        <Navbar />
        <main class="max-w-4xl mx-auto px-6 py-8">
            <h1 class="text-2xl font-bold text-gray-900 mb-6">Flash Messages &amp; Validation</h1>

            <!-- Flash display -->
            <div v-if="flash?.success" class="mb-6 bg-green-50 border border-green-200 rounded-lg p-4">
                <p class="text-green-800 font-medium">{{ flash.success }}</p>
            </div>

            <!-- Errors display -->
            <div v-if="hasErrors" class="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
                <p class="text-red-800 font-medium mb-2">Validation Errors:</p>
                <ul class="text-red-600 text-sm list-disc pl-5">
                    <li v-for="(msg, field) in errors" :key="field">
                        <strong>{{ field }}:</strong> {{ msg }}
                    </li>
                </ul>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <!-- Flash message form -->
                <div class="bg-white rounded-lg p-6 shadow-sm border">
                    <h2 class="text-lg font-semibold text-gray-900 mb-4">Send Flash Message</h2>
                    <form @submit.prevent="form.post('/features/flash/success')">
                        <input
                            v-model="form.message"
                            type="text"
                            placeholder="Your message..."
                            class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm mb-3"
                        />
                        <button type="submit" class="w-full bg-green-600 text-white rounded-lg px-4 py-2 text-sm hover:bg-green-700 transition">
                            Flash Success
                        </button>
                    </form>
                    <p class="mt-3 text-xs text-gray-400">Uses .WithFlash("success", message) on redirect</p>
                </div>

                <!-- Validation error form -->
                <div class="bg-white rounded-lg p-6 shadow-sm border">
                    <h2 class="text-lg font-semibold text-gray-900 mb-4">Trigger Validation Errors</h2>
                    <form @submit.prevent="router.post('/features/flash/error')">
                        <p class="text-sm text-gray-600 mb-3">
                            Simulates a form submission with email and name errors.
                        </p>
                        <button type="submit" class="w-full bg-red-600 text-white rounded-lg px-4 py-2 text-sm hover:bg-red-700 transition">
                            Submit with Errors
                        </button>
                    </form>
                    <p class="mt-3 text-xs text-gray-400">Uses .WithErrors() on redirect</p>
                </div>

                <!-- Error bag form -->
                <div class="bg-white rounded-lg p-6 shadow-sm border md:col-span-2">
                    <h2 class="text-lg font-semibold text-gray-900 mb-4">Error Bags</h2>
                    <p class="text-sm text-gray-600 mb-3">
                        Error bags allow multiple forms on one page with independent validation.
                    </p>
                    <form @submit.prevent="router.post('/features/flash/error-bag')">
                        <button type="submit" class="bg-orange-600 text-white rounded-lg px-4 py-2 text-sm hover:bg-orange-700 transition">
                            Trigger "register" Error Bag
                        </button>
                    </form>
                    <p class="mt-3 text-xs text-gray-400">Uses .WithErrors(errors, errorBag: "register")</p>
                </div>
            </div>
        </main>
    </div>
</template>
