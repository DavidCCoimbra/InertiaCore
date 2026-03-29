import { createApp, h } from 'vue'
import { createInertiaApp } from '@inertiajs/vue3'
import { resolvePageComponent, resolvePageData, initInertiaCore } from '@inertiacore/vite/inertia-helpers'
import { initSignalR } from './plugins/signalr'
import './app.css'

const initialPage = await resolvePageData()

createInertiaApp({
    page: initialPage,
    resolve: name => resolvePageComponent(
        `./pages/${name}.vue`,
        import.meta.glob('./pages/**/*.vue'),
    ),
    setup({ el, App, props, plugin }) {
        createApp({ render: () => h(App, props) })
            .use(plugin)
            .mount(el)

        // Initialize InertiaCore client features (timed props, etc.)
        initInertiaCore()

        // Initialize SignalR for live props
        initSignalR()
    },
})
