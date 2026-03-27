import * as net from 'node:net';
import * as fs from 'node:fs';
import { decode } from '@msgpack/msgpack';
import type { Page, RenderFunction, SsrResponse, ServerOptions } from './types.js';

const DEFAULT_SOCKET_PATH = '/tmp/inertia-ssr.sock';
const MAX_MESSAGE_SIZE = 10 * 1024 * 1024; // 10MB

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

            // Process all complete messages in the buffer
            while (buffer.length >= 4) {
                const messageLength = buffer.readInt32LE(0);

                // Guard against malformed or oversized messages
                if (messageLength <= 0 || messageLength > MAX_MESSAGE_SIZE) {
                    console.error(`[inertiacore/ssr] Invalid message length: ${messageLength}`);
                    buffer = Buffer.alloc(0);
                    connection.destroy();
                    return;
                }

                // Wait for the full payload to arrive
                if (buffer.length < 4 + messageLength) return;

                const payload = buffer.subarray(4, 4 + messageLength);
                buffer = buffer.subarray(4 + messageLength);

                try {
                    const page = decode(payload) as Page;
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
