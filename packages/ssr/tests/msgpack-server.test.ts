import { afterEach, describe, expect, it, vi } from 'vitest'
import * as net from 'node:net'
import * as fs from 'node:fs'
import { encode } from '@msgpack/msgpack'
import { createMsgpackServer } from '../src/msgpack-server.js'
import type { Page, SsrResponse } from '../src/types.js'

const TEST_SOCKET = '/tmp/inertia-ssr-test.sock'

function cleanSocket() {
    try { fs.unlinkSync(TEST_SOCKET) } catch { /* ignore */ }
}

function mockRender(page: Page): Promise<SsrResponse> {
    return Promise.resolve({
        head: [`<title>${page.component}</title>`],
        body: `<div id="app">${page.component}</div>`,
    })
}

function sendMessage(socket: net.Socket, data: Uint8Array): void {
    const lengthBuf = Buffer.alloc(4)
    lengthBuf.writeInt32LE(data.length)
    socket.write(lengthBuf)
    socket.write(data)
}

function readResponse(socket: net.Socket): Promise<SsrResponse> {
    return new Promise((resolve, reject) => {
        let buffer = Buffer.alloc(0)

        socket.on('data', (chunk: Buffer) => {
            buffer = Buffer.concat([buffer, chunk])

            if (buffer.length < 4) return

            const messageLength = buffer.readInt32LE(0)
            if (buffer.length < 4 + messageLength) return

            const payload = buffer.subarray(4, 4 + messageLength)
            resolve(JSON.parse(payload.toString('utf-8')))
        })

        socket.on('error', reject)
    })
}

function connectToSocket(path: string): Promise<net.Socket> {
    return new Promise((resolve, reject) => {
        const socket = net.createConnection({ path })
        socket.on('connect', () => resolve(socket))
        socket.on('error', reject)
    })
}

function waitForServer(server: net.Server): Promise<void> {
    return new Promise((resolve) => {
        if (server.listening) {
            resolve()
        } else {
            server.once('listening', () => resolve())
        }
    })
}

describe('createMsgpackServer', () => {
    let server: net.Server | null = null

    afterEach(() => {
        if (server) {
            server.close()
            server = null
        }
        cleanSocket()
        vi.restoreAllMocks()
    })

    it('starts listening on the specified socket path', async () => {
        server = createMsgpackServer(mockRender, { socketPath: TEST_SOCKET })
        await waitForServer(server)

        expect(fs.existsSync(TEST_SOCKET)).toBe(true)
    })

    it('renders a page and returns the response', async () => {
        server = createMsgpackServer(mockRender, { socketPath: TEST_SOCKET })
        await waitForServer(server)

        const socket = await connectToSocket(TEST_SOCKET)

        const page: Page = {
            component: 'Home',
            props: { title: 'Hello' },
            url: '/',
            version: '1',
        }

        sendMessage(socket, encode(page) as Uint8Array)
        const response = await readResponse(socket)

        expect(response.head).toEqual(['<title>Home</title>'])
        expect(response.body).toBe('<div id="app">Home</div>')

        socket.destroy()
    })

    it('handles render errors gracefully', async () => {
        const errorRender = () => Promise.reject(new Error('render failed'))
        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

        server = createMsgpackServer(errorRender, { socketPath: TEST_SOCKET })
        await waitForServer(server)

        const socket = await connectToSocket(TEST_SOCKET)

        const page: Page = {
            component: 'Broken',
            props: {},
            url: '/broken',
            version: null,
        }

        sendMessage(socket, encode(page) as Uint8Array)
        const response = await readResponse(socket)

        expect(response.head).toEqual([])
        expect(response.body).toBe('')
        expect(consoleSpy).toHaveBeenCalledWith(
            expect.stringContaining('render failed'),
        )

        socket.destroy()
    })

    it('cleans up stale socket file on startup', async () => {
        // Create a stale socket file
        fs.writeFileSync(TEST_SOCKET, '')
        expect(fs.existsSync(TEST_SOCKET)).toBe(true)

        server = createMsgpackServer(mockRender, { socketPath: TEST_SOCKET })
        await waitForServer(server)

        // Should still be listening (cleaned up the stale file and created a new one)
        const socket = await connectToSocket(TEST_SOCKET)
        socket.destroy()
    })

    it('handles multiple sequential requests on the same connection', async () => {
        server = createMsgpackServer(mockRender, { socketPath: TEST_SOCKET })
        await waitForServer(server)

        const socket = await connectToSocket(TEST_SOCKET)

        // First request
        const page1: Page = { component: 'Page1', props: {}, url: '/1', version: null }
        sendMessage(socket, encode(page1) as Uint8Array)
        const response1 = await readResponse(socket)
        expect(response1.body).toBe('<div id="app">Page1</div>')

        socket.destroy()

        // Second request on a new connection
        const socket2 = await connectToSocket(TEST_SOCKET)
        const page2: Page = { component: 'Page2', props: {}, url: '/2', version: null }
        sendMessage(socket2, encode(page2) as Uint8Array)
        const response2 = await readResponse(socket2)
        expect(response2.body).toBe('<div id="app">Page2</div>')

        socket2.destroy()
    })

    it('handles malformed length prefix gracefully', async () => {
        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

        server = createMsgpackServer(mockRender, { socketPath: TEST_SOCKET })
        await waitForServer(server)

        const socket = await connectToSocket(TEST_SOCKET)

        // Send a negative length prefix
        const badLength = Buffer.alloc(4)
        badLength.writeInt32LE(-1)
        socket.write(badLength)

        // Give it a moment to process
        await new Promise(resolve => setTimeout(resolve, 50))

        expect(consoleSpy).toHaveBeenCalledWith(
            expect.stringContaining('Invalid message length'),
        )

        socket.destroy()
    })

    it('uses default socket path when no options provided', async () => {
        // Clean default socket in case it exists
        try { fs.unlinkSync('/tmp/inertia-ssr.sock') } catch { /* ignore */ }

        server = createMsgpackServer(mockRender)
        await waitForServer(server)

        expect(fs.existsSync('/tmp/inertia-ssr.sock')).toBe(true)

        server.close()
        server = null
        try { fs.unlinkSync('/tmp/inertia-ssr.sock') } catch { /* ignore */ }
    })
})
