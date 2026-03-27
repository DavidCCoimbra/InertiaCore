import { describe, expect, it } from 'vitest'

// Test the SERVER_IMPORT_RE pattern used by the intercept plugin.
// Copied from dev-server.ts since it's a module-level constant.
const SERVER_IMPORT_RE = /^@inertiajs\/[\w-]+\/(server|dist\/server)(\.js)?$/

describe('SERVER_IMPORT_RE', () => {
    it('matches @inertiajs/vue3/server', () => {
        expect(SERVER_IMPORT_RE.test('@inertiajs/vue3/server')).toBe(true)
    })

    it('matches @inertiajs/react/server', () => {
        expect(SERVER_IMPORT_RE.test('@inertiajs/react/server')).toBe(true)
    })

    it('matches @inertiajs/svelte/server', () => {
        expect(SERVER_IMPORT_RE.test('@inertiajs/svelte/server')).toBe(true)
    })

    it('matches @inertiajs/core/server', () => {
        expect(SERVER_IMPORT_RE.test('@inertiajs/core/server')).toBe(true)
    })

    it('matches @inertiajs/core/dist/server', () => {
        expect(SERVER_IMPORT_RE.test('@inertiajs/core/dist/server')).toBe(true)
    })

    it('matches @inertiajs/core/dist/server.js', () => {
        expect(SERVER_IMPORT_RE.test('@inertiajs/core/dist/server.js')).toBe(true)
    })

    it('matches @inertiajs/vue3/server.js', () => {
        expect(SERVER_IMPORT_RE.test('@inertiajs/vue3/server.js')).toBe(true)
    })

    it('matches hypothetical future adapters', () => {
        expect(SERVER_IMPORT_RE.test('@inertiajs/solid/server')).toBe(true)
        expect(SERVER_IMPORT_RE.test('@inertiajs/preact/server')).toBe(true)
        expect(SERVER_IMPORT_RE.test('@inertiajs/angular-adapter/server')).toBe(true)
    })

    it('does not match non-server imports', () => {
        expect(SERVER_IMPORT_RE.test('@inertiajs/vue3')).toBe(false)
        expect(SERVER_IMPORT_RE.test('@inertiajs/core')).toBe(false)
        expect(SERVER_IMPORT_RE.test('@inertiajs/vue3/client')).toBe(false)
    })

    it('does not match other packages', () => {
        expect(SERVER_IMPORT_RE.test('@other/vue3/server')).toBe(false)
        expect(SERVER_IMPORT_RE.test('inertiajs/vue3/server')).toBe(false)
    })

    it('does not match nested paths beyond dist/server', () => {
        expect(SERVER_IMPORT_RE.test('@inertiajs/core/dist/server/index')).toBe(false)
        expect(SERVER_IMPORT_RE.test('@inertiajs/core/src/server')).toBe(false)
    })
})
