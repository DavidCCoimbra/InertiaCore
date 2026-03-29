import * as signalR from '@microsoft/signalr'
import { router, usePage } from '@inertiajs/vue3'

let connection: signalR.HubConnection | null = null
let currentComponent: string | null = null
let currentChannels: string[] = []

export function initSignalR() {
    if (typeof window === 'undefined') return
    if (connection) return

    connection = new signalR.HubConnectionBuilder()
        .withUrl('/inertia-hub')
        .withAutomaticReconnect()
        .build()

    // RefreshProps: server says "go fetch these props via HTTP"
    connection.on('inertia:reload', (data: { component?: string; only?: string[] }) => {
        router.reload({ only: data.only })
    })

    // PushProps: server sends prop values directly — page-scoped
    connection.on('inertia:props', (data: { component?: string; props?: Record<string, unknown> }) => {
        if (!data.props) return
        const page = usePage()
        Object.assign(page.props, data.props)
    })

    // PushToChannel: server sends prop values to a channel — cross-page
    // Maps channel data to the props subscribed to that channel via [InertiaLive]
    connection.on('inertia:channel', (data: { channel?: string; props?: Record<string, unknown> }) => {
        if (!data.channel || !data.props) return
        const page = usePage()

        // liveProps metadata: maps prop names to channels
        // It's in the page-level metadata, accessible via usePage() in Inertia v3
        const liveProps = getLivePropsMetadata()
        if (!liveProps) return

        // Merge values for props subscribed to this channel
        for (const [propName, channel] of Object.entries(liveProps)) {
            if (channel === data.channel && propName in data.props) {
                ;(page.props as any)[propName] = data.props[propName]
            }
        }
    })

    connection.start().then(() => {
        subscribeToCurrentPage()
    }).catch(err => {
        console.warn('[inertiacore/signalr] Connection failed:', err.message)
    })

    // Re-subscribe on navigation
    router.on('navigate', () => {
        subscribeToCurrentPage()
    })
}

function getLivePropsMetadata(): Record<string, string> | null {
    try {
        const el = document.querySelector('script[data-page][type="application/json"]')
        if (!el?.textContent) return null
        const page = JSON.parse(el.textContent)
        return page.liveProps ?? null
    } catch {
        return null
    }
}

function subscribeToCurrentPage() {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) return

    const page = usePage()
    const component = page.component

    // Subscribe to component group
    if (component && component !== currentComponent) {
        if (currentComponent) {
            connection.invoke('UnsubscribeFromComponent', currentComponent).catch(e => console.warn('[inertiacore/signalr]', e.message))
        }
        connection.invoke('SubscribeToComponent', component).catch(e => console.warn('[inertiacore/signalr]', e.message))
        currentComponent = component
    }

    // Subscribe to live prop channels from page metadata
    const liveProps = getLivePropsMetadata()
    const newChannels = liveProps ? [...new Set(Object.values(liveProps))] : []

    // Unsubscribe from channels no longer needed
    for (const ch of currentChannels) {
        if (!newChannels.includes(ch)) {
            connection.invoke('LeaveGroup', ch).catch(e => console.warn('[inertiacore/signalr]', e.message))
        }
    }

    // Subscribe to new channels
    for (const ch of newChannels) {
        if (!currentChannels.includes(ch)) {
            connection.invoke('JoinGroup', ch).catch(e => console.warn('[inertiacore/signalr]', e.message))
        }
    }

    currentChannels = newChannels
}
