import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { router } from '@inertiajs/core';

export interface InertiaSignalROptions {
    /** SignalR hub URL. Default: /inertia-hub */
    hubUrl?: string;
    /** Automatically subscribe to the current page's component. Default: true */
    autoSubscribe?: boolean;
    /** SignalR log level. Default: Warning */
    logLevel?: LogLevel;
}

export interface ReloadMessage {
    component: string;
    only?: string[];
}

/**
 * Creates a SignalR connection that listens for prop refresh signals
 * and triggers Inertia partial reloads automatically.
 *
 * @example
 * ```ts
 * import { createInertiaSignalR } from '@inertiacore/signalr';
 *
 * // In your app setup (app.ts)
 * createInertiaSignalR();
 *
 * // Or with options
 * createInertiaSignalR({
 *     hubUrl: '/my-hub',
 *     autoSubscribe: true,
 * });
 * ```
 */
export function createInertiaSignalR(options: InertiaSignalROptions = {}): HubConnection {
    const {
        hubUrl = '/inertia-hub',
        autoSubscribe = true,
        logLevel = LogLevel.Warning,
    } = options;

    const connection = new HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .configureLogging(logLevel)
        .build();

    // Listen for reload signals from the server
    connection.on('inertia:reload', (message: ReloadMessage) => {
        const currentComponent = getCurrentComponent();

        // Only reload if the signal targets the current component
        if (currentComponent && message.component === currentComponent) {
            router.reload({
                only: message.only,
            });
        }
    });

    // Start connection
    connection.start().catch((err) => {
        console.error('[inertiacore/signalr] Connection failed:', err);
    });

    // Auto-subscribe to component changes
    if (autoSubscribe) {
        let currentComponent: string | null = null;

        // Subscribe on navigation
        router.on('navigate', (event) => {
            const newComponent = (event.detail?.page as any)?.component;
            if (!newComponent || newComponent === currentComponent) return;

            // Unsubscribe from previous
            if (currentComponent) {
                connection.invoke('UnsubscribeFromComponent', currentComponent).catch(() => {});
            }

            // Subscribe to new
            currentComponent = newComponent;
            connection.invoke('SubscribeToComponent', currentComponent).catch(() => {});
        });
    }

    return connection;
}

function getCurrentComponent(): string | null {
    // Read from the page data in the DOM
    const scriptEl = document.querySelector('script[data-page][type="application/json"]');
    if (scriptEl?.textContent) {
        try {
            return JSON.parse(scriptEl.textContent).component;
        } catch {
            return null;
        }
    }
    return null;
}
