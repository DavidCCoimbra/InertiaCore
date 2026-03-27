import * as net from 'node:net';
import * as fs from 'node:fs';
import { decode } from '@msgpack/msgpack';
import type { Page, RenderFunction, SsrResponse, ServerOptions } from './types.js';

const DEFAULT_SOCKET_PATH = '/tmp/inertia-ssr.sock';

/**
 * Creates an SSR server that receives MessagePack-encoded page objects
 * over a Unix Domain Socket. Drop-in replacement for Inertia's standard
 * HTTP SSR server with ~4x faster IPC.
 *
 * @param render - The render function (same as standard SSR)
 * @param options - Server options (socket path, etc.)
 */
export function createMsgpackServer(
    render: RenderFunction,
    options: ServerOptions = {},
): net.Server {
    const socketPath = options.socketPath ?? DEFAULT_SOCKET_PATH;

    // Clean up stale socket file from previous run
    try {
        fs.unlinkSync(socketPath);
    } catch {
        // File doesn't exist — that's fine
    }

    const server = net.createServer((connection) => {
        let buffer = Buffer.alloc(0);

        connection.on('data', async (chunk: Buffer) => {
            buffer = Buffer.concat([buffer, chunk]);

            // Wait for at least the 4-byte length prefix
            if (buffer.length < 4) return;

            const messageLength = buffer.readInt32LE(0);
            if (buffer.length < 4 + messageLength) return;

            // Extract the MessagePack payload
            const payload = buffer.subarray(4, 4 + messageLength);
            buffer = buffer.subarray(4 + messageLength);

            try {
                // Decode MessagePack → JavaScript object
                const page = decode(payload) as Page;

                // Render the component (same render function as standard SSR)
                const result: SsrResponse = await render(page);

                sendResponse(connection, result);
            } catch (error) {
                const message = error instanceof Error ? error.message : String(error);
                console.error(`[inertiacore/ssr] Render error: ${message}`);

                sendResponse(connection, {
                    head: [],
                    body: '',
                });
            }
        });

        connection.on('error', (err) => {
            console.error(`[inertiacore/ssr] Connection error: ${err.message}`);
        });
    });

    server.listen(socketPath, () => {
        console.log(`Inertia SSR (msgpack) listening on ${socketPath}`);
    });

    // Graceful shutdown
    const shutdown = () => {
        server.close(() => {
            try {
                fs.unlinkSync(socketPath);
            } catch {
                // Ignore
            }
            process.exit(0);
        });
    };

    process.on('SIGTERM', shutdown);
    process.on('SIGINT', shutdown);

    return server;
}

function sendResponse(connection: net.Socket, response: SsrResponse): void {
    const json = JSON.stringify(response);
    const responseBuf = Buffer.from(json, 'utf-8');

    // Write length-prefixed response
    const lengthBuf = Buffer.alloc(4);
    lengthBuf.writeInt32LE(responseBuf.length);

    connection.write(lengthBuf);
    connection.write(responseBuf);
}
