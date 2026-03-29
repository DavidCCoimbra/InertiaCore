export async function resolvePageComponent<T>(path: string|string[], pages: Record<string, Promise<T> | (() => Promise<T>)>): Promise<T> {
    for (const p of (Array.isArray(path) ? path : [path])) {
        const page = pages[p]

        if (typeof page === 'undefined') {
            continue
        }

        return typeof page === 'function' ? page() : page
    }

    throw new Error(`Page not found: ${path}`)
}

declare global {
    interface Window {
        __inertiaPageData?: Promise<Record<string, unknown>>
    }
}

/**
 * Resolves the initial page data for Inertia hydration.
 * When async page data is enabled (SSR + AsyncPageData), the full props are fetched
 * via a parallel HTTP request instead of being embedded in the HTML.
 * Falls back to reading from the `<script data-page>` tag when async is not active.
 */
export async function resolvePageData(id = 'app'): Promise<Record<string, unknown>> {
    const el = document.querySelector(`script[data-page="${id}"][type="application/json"]`)

    if (!el?.textContent) {
        throw new Error(`Inertia page data script not found for #${id}`)
    }

    const page = JSON.parse(el.textContent) as Record<string, unknown>

    // Async page data: server started the fetch, we await it.
    // Falls back to the minimal page data if the fetch fails.
    if (typeof window.__inertiaPageData !== 'undefined') {
        try {
            return await window.__inertiaPageData
        } catch {
            console.warn('[inertiacore] Async page data fetch failed, using inline page data')
        }
    }

    return page
}

/**
 * Initializes InertiaCore client-side features:
 * - Reads `timedProps` metadata from the page object and auto-polls.
 * - Handles server-declared polling intervals via [InertiaTimed(IntervalSeconds = N)].
 *
 * Call once in your app setup. Works globally — no per-component imports needed.
 * The server declares the intervals, the client obeys automatically.
 *
 * @example
 * ```ts
 * import { initInertiaCore } from '@inertiacore/vite/inertia-helpers'
 *
 * createInertiaApp({
 *     setup({ el, App, props, plugin }) {
 *         createApp({ render: () => h(App, props) }).use(plugin).mount(el)
 *         initInertiaCore()
 *     },
 * })
 * ```
 */
export function initInertiaCore(): void {
    if (typeof window === 'undefined') return

    let timers: ReturnType<typeof setInterval>[] = []

    // @ts-ignore — peer dependency, resolved from consuming app's node_modules
    import('@inertiajs/vue3').then(({ router }: any) => {
        const setupTimedProps = () => {
            // Clear previous timers
            timers.forEach(t => clearInterval(t))
            timers = []

            // Read timedProps from the page's script tag
            const el = document.querySelector('script[data-page][type="application/json"]')
            if (!el?.textContent) return

            try {
                const page = JSON.parse(el.textContent)
                const timedProps = page.timedProps as Record<string, number> | undefined
                if (!timedProps) return

                // Group props by interval for efficiency
                const byInterval = new Map<number, string[]>()
                for (const [prop, intervalMs] of Object.entries(timedProps)) {
                    const existing = byInterval.get(intervalMs) ?? []
                    existing.push(prop)
                    byInterval.set(intervalMs, existing)
                }

                for (const [intervalMs, props] of byInterval) {
                    timers.push(setInterval(() => {
                        router.reload({ only: props })
                    }, intervalMs))
                }
            } catch { /* ignore parse errors */ }
        }

        // Setup on initial load
        setupTimedProps()

        // Re-setup on every navigation (new page may have different timedProps)
        router.on('navigate', () => setupTimedProps())
    })
}

/**
 * @deprecated Use `initInertiaCore()` instead — it handles timedProps globally.
 */
export const useTimedProps = initInertiaCore
