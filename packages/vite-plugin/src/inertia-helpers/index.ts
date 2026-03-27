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
