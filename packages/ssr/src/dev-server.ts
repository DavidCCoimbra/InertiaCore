import * as http from 'node:http';
import type { Page, SsrResponse } from './types.js';

export interface DevServerOptions {
    /** Port the SSR dev server listens on. @default 13714 */
    port?: number;
    /** Vite config file path. @default 'vite.config.ts' */
    configFile?: string;
    /** SSR entry module path. @default './ClientApp/ssr.ts' */
    ssrEntry?: string;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const _global: Record<string, any> = globalThis as any;

const RENDER_KEY = '__inertiacore_ssr_render';

// Virtual module ID used to intercept @inertiajs server imports.
// Instead of starting an HTTP server, the mock captures the render callback.
const VIRTUAL_SERVER_ID = '\0inertiacore-ssr-dev-server-mock';

// Matches any @inertiajs framework adapter's server export:
// @inertiajs/vue3/server, @inertiajs/react/server, @inertiajs/solid/server, etc.
const SERVER_IMPORT_RE = /^@inertiajs\/[\w-]+\/(server|dist\/server)(\.js)?$/;

// Vite plugin that intercepts @inertiajs server imports.
// Replaces the real createServer (which starts an HTTP server) with a mock
// that captures the render callback for our dev server to use.
function ssrServerInterceptPlugin() {
    return {
        name: 'inertiacore:ssr-dev-intercept',
        enforce: 'pre' as const,
        resolveId(id: string) {
            if (SERVER_IMPORT_RE.test(id)) {
                return VIRTUAL_SERVER_ID;
            }
            return null;
        },
        load(id: string) {
            if (id === VIRTUAL_SERVER_ID) {
                // Return a mock that captures the render callback instead of starting a server
                return `
                    export default function createServer(renderCallback, port) {
                        globalThis.${RENDER_KEY} = renderCallback;
                    }
                `;
            }
            return null;
        },
    };
}

/**
 * Creates an SSR dev server powered by Vite's ssrLoadModule.
 * Modules are transformed on-the-fly — zero rebuild on file changes.
 * Vite invalidates only the changed module, next request gets fresh code.
 *
 * The SSR entry file (e.g. ssr.ts) can use the standard @inertiajs createServer()
 * pattern — the dev server intercepts it and captures the render callback.
 *
 * @example
 * ```ts
 * import { createDevServer } from '@inertiacore/ssr';
 *
 * createDevServer({
 *     ssrEntry: './ClientApp/ssr.ts',
 *     port: 13714,
 * });
 * ```
 */
export async function createDevServer(options: DevServerOptions = {}): Promise<http.Server> {
    const {
        port = 13714,
        configFile,
        ssrEntry = './ClientApp/ssr.ts',
    } = options;

    // Dynamic import to avoid requiring vite as a hard dependency
    const { createServer: createViteServer } = await import('vite');

    const vite = await createViteServer({
        configFile,
        server: { middlewareMode: true },
        appType: 'custom',
        plugins: [ssrServerInterceptPlugin()],
        ssr: {
            // Prevent Vite from externalizing @inertiajs packages so our
            // intercept plugin can replace the server module import
            noExternal: [/^@inertiajs\//],
        },
    });

    // Load the SSR entry once at startup to capture the render callback.
    // The intercepted createServer() stores the callback on globalThis.
    await vite.ssrLoadModule(ssrEntry);

    if (!_global[RENDER_KEY]) {
        throw new Error(
            `SSR entry '${ssrEntry}' did not call createServer() from @inertiajs. ` +
            `Make sure your ssr.ts calls createServer((page) => createInertiaApp({...})).`
        );
    }

    const server = http.createServer(async (req, res) => {
        // Health check endpoint
        if (req.url === '/health-check' || req.url === '/health') {
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ status: 'ok' }));
            return;
        }

        // SSR render endpoint
        if (req.method === 'POST' && req.url === '/render') {
            const chunks: Buffer[] = [];
            let bodySize = 0;
            const maxBodySize = 10 * 1024 * 1024; // 10MB

            req.on('data', (chunk: Buffer) => {
                bodySize += chunk.length;
                if (bodySize > maxBodySize) {
                    res.writeHead(413, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({ head: [], body: '' }));
                    req.destroy();
                    return;
                }
                chunks.push(chunk);
            });

            req.on('end', async () => {
                try {
                    const body = Buffer.concat(chunks).toString('utf-8');
                    const page: Page = JSON.parse(body);

                    // Re-load the module to pick up any changes Vite has invalidated
                    await vite.ssrLoadModule(ssrEntry);
                    const render = _global[RENDER_KEY] as (page: Page) => Promise<SsrResponse>;

                    const result = await render(page);

                    res.writeHead(200, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify(result));
                } catch (error) {
                    const message = error instanceof Error ? error.message : String(error);
                    const stack = error instanceof Error ? error.stack : undefined;

                    console.error(`[inertiacore/ssr-dev] Render error: ${message}`);
                    if (stack) console.error(stack);

                    // Fix stack trace for Vite-transformed modules
                    if (error instanceof Error) {
                        vite.ssrFixStacktrace(error);
                    }

                    res.writeHead(500, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({ head: [], body: '' }));
                }
            });

            return;
        }

        res.writeHead(404);
        res.end();
    });

    server.listen(port, '127.0.0.1', () => {
        console.log(`Inertia SSR dev server (Vite ssrLoadModule) listening on http://localhost:${port}`);
        console.log(`  Entry: ${ssrEntry}`);
        console.log(`  Modules are transformed on-the-fly — zero rebuild on changes`);
    });

    // Graceful shutdown
    const shutdown = () => {
        vite.close();
        server.close();
        process.exit(0);
    };

    process.on('SIGTERM', shutdown);
    process.on('SIGINT', shutdown);

    return server;
}
