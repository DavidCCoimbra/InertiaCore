import fs from 'fs'
import path from 'path'
import { spawn, type ChildProcess } from 'child_process'
import { createConnection } from 'net'
import colors from 'picocolors'
import type { LauncherConfig, SsrDevConfig } from './config.js'

let dotnetProcess: ChildProcess | null = null
let ssrProcess: ChildProcess | null = null
let ssrBuildProcess: ChildProcess | null = null

// Build error pattern: CS followed by 4 digits
const buildErrorPattern = /\b(CS\d{4}|error\s+CS\d{4})/

// Purple color for .NET-themed log lines
const net = (s: string) => `\x1b[38;2;204;110;212m${s}\x1b[0m`

/**
 * Check if a port is already in use.
 */
export async function isPortInUse(port: number, host = '127.0.0.1'): Promise<boolean> {
    return new Promise((resolve) => {
        const socket = createConnection({ port, host })
        socket.on('connect', () => { socket.destroy(); resolve(true) })
        socket.on('error', () => { socket.destroy(); resolve(false) })
        socket.setTimeout(500, () => { socket.destroy(); resolve(false) })
    })
}

/**
 * Extract port from a URL string.
 */
function extractPort(url: string): number | undefined {
    try {
        return new URL(url).port ? parseInt(new URL(url).port) : undefined
    } catch {
        return undefined
    }
}

/**
 * Start the .NET server as a child process.
 * Detects port conflicts, surfaces build errors, and pipes output to the Vite logger.
 */
export async function startDotnetServer(
    config: boolean | LauncherConfig,
    logger: import('vite').Logger,
    appUrl?: string,
): Promise<void> {
    if (config === false || dotnetProcess) return

    const opts: LauncherConfig = typeof config === 'object' ? config : {}
    const command = opts.command ?? 'dotnet'
    const args = opts.args ?? ['watch', 'run']
    const cwd = opts.cwd ?? process.cwd()

    // Port conflict detection — skip launcher if already running
    if (appUrl) {
        const port = extractPort(appUrl)
        if (port && await isPortInUse(port)) {
            logger.info(`  ${net('➜')}  ${net(colors.bold('App'))}:  ${colors.cyan(appUrl)}  ${colors.green('✓ already running')} ${colors.dim('(skipping launcher)')}`)
            return
        }
    }

    logger.info(`  ${net('➜')}  ${net(colors.bold('Launching'))}:  ${colors.dim(`${command} ${args.join(' ')}`)}`)

    dotnetProcess = spawn(command, args, {
        cwd,
        stdio: ['ignore', 'pipe', 'pipe'],
        env: {
            ...process.env,
            DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER: '1',
            DOTNET_WATCH: '1',
        },
    })

    dotnetProcess.stdout?.on('data', (data: Buffer) => {
        const lines = data.toString().trim().split('\n')
        for (const line of lines) {
            if (!line.trim()) continue
            formatDotnetOutput(logger, line.trim())
        }
    })

    dotnetProcess.stderr?.on('data', (data: Buffer) => {
        const lines = data.toString().trim().split('\n')
        for (const line of lines) {
            if (!line.trim()) continue
            formatDotnetOutput(logger, line.trim())
        }
    })

    dotnetProcess.on('exit', (code) => {
        if (code !== null && code !== 0) {
            logger.error(`  ${colors.red('[dotnet]')} Process exited with code ${code}`)
        }
        dotnetProcess = null
    })
}

/**
 * Format dotnet output — highlight build errors clearly.
 */
function formatDotnetOutput(logger: import('vite').Logger, line: string): void {
    if (buildErrorPattern.test(line)) {
        // Build error — make it stand out
        logger.error(`  ${colors.red('✗')}  ${colors.red(line)}`)
    } else if (line.includes('Build succeeded')) {
        logger.info(`  ${colors.green('✓')}  ${colors.dim('[dotnet]')} ${colors.green(line)}`)
    } else if (line.includes('Build FAILED')) {
        logger.error(`  ${colors.red('✗')}  ${colors.red('[dotnet]')} ${colors.red(line)}`)
    } else {
        logger.info(`  ${colors.dim('[dotnet]')} ${line}`)
    }
}

/**
 * Check if @inertiacore/ssr is installed by looking in node_modules.
 */
function hasSsrPackage(): boolean {
    return fs.existsSync(path.join(process.cwd(), 'node_modules', '@inertiacore', 'ssr'))
}

/**
 * Start SSR development mode.
 * - If @inertiacore/ssr is installed → Nuxt-style dev server (Vite ssrLoadModule, zero rebuild)
 * - If not installed → fallback to vite build --ssr --watch + node --watch (rebuild on changes)
 */
export function startSsrSidecar(
    config: boolean | SsrDevConfig,
    logger: import('vite').Logger,
): void {
    if (config === false || ssrProcess) return

    const opts: SsrDevConfig = typeof config === 'object' ? config : {}
    const port = opts.port ?? 13714

    // Check if SSR sidecar is already running
    const portCheck = createConnection({ port, host: '127.0.0.1' })
    portCheck.on('connect', () => {
        portCheck.destroy()
        logger.info(`  ${net('➜')}  ${net(colors.bold('SSR'))}:   ${colors.cyan(`http://127.0.0.1:${port}`)}  ${colors.green('✓ already running')} ${colors.dim('(skipping)')}`)
    })
    portCheck.on('error', () => {
        portCheck.destroy()

        if (opts.v8) {
            // V8 mode: build + signal, no Node.js sidecar
            startSsrBuildWatch(opts, port, logger)
        } else if (hasSsrPackage()) {
            startSsrDevServer(opts, port, logger)
        } else {
            startSsrBuildWatch(opts, port, logger)
        }
    })
    portCheck.setTimeout(500, () => { portCheck.destroy() })
}

/**
 * Nuxt-style: @inertiacore/ssr createDevServer with Vite ssrLoadModule.
 * Zero rebuild — modules transformed on-the-fly.
 */
function startSsrDevServer(opts: SsrDevConfig, port: number, logger: import('vite').Logger): void {
    const ssrEntry = opts.entry ?? './ClientApp/ssr.ts'

    logger.info(`  ${net('➜')}  ${net(colors.bold('SSR'))}:   ${colors.dim(`Vite ssrLoadModule dev server on :${port}`)} ${colors.green('(zero rebuild)')}`)

    const script = `
        import { createDevServer } from '@inertiacore/ssr';
        createDevServer({
            port: ${port},
            ssrEntry: ${JSON.stringify(ssrEntry)},
        });
    `

    ssrProcess = spawn('node', ['--input-type=module', '-e', script], {
        cwd: process.cwd(),
        stdio: ['ignore', 'pipe', 'pipe'],
        env: { ...process.env, INERTIACORE_SSR_DEV: '1' },
    })

    pipeOutput(ssrProcess, '[ssr]', logger)
}

/**
 * Fallback: vite build --ssr --watch + node --watch.
 * Rebuilds SSR bundle on file changes (~120ms per change).
 */
function startSsrBuildWatch(opts: SsrDevConfig, port: number, logger: import('vite').Logger): void {
    const script = 'dist/ssr/ssr.js'
    const v8Opts = typeof opts.v8 === 'object' ? opts.v8 : {}
    const reloadUrl = opts.v8 ? (v8Opts.reloadUrl ?? 'http://localhost:5274/inertia/ssr-reload') : undefined

    if (reloadUrl) {
        logger.info(`  ${net('➜')}  ${net(colors.bold('SSR'))}:   ${colors.dim('vite build --ssr --watch')} ${colors.green('(EmbeddedV8)')}`)
        logger.info(`  ${net('➜')}  ${net(colors.bold('SSR'))}:   ${colors.dim(`V8 reload signal → ${reloadUrl}`)}`)
    } else {
        logger.info(`  ${net('➜')}  ${net(colors.bold('SSR'))}:   ${colors.dim('vite build --ssr --watch')} ${colors.yellow('(@inertiacore/ssr not installed — using rebuild mode)')}`)
    }

    ssrBuildProcess = spawn('npx', ['vite', 'build', '--ssr', '--watch'], {
        cwd: process.cwd(),
        stdio: ['ignore', 'pipe', 'pipe'],
        env: process.env,
    })

    let sidecarStarted = false

    ssrBuildProcess.stdout?.on('data', (data: Buffer) => {
        const text = data.toString().trim()
        if (!text) return

        for (const line of text.split('\n')) {
            if (line.trim()) logger.info(`  ${colors.dim('[ssr:build]')} ${line.trim()}`)
        }

        if (text.includes('built in') || text.includes('✓')) {
            if (reloadUrl) {
                // Signal V8 engine pool to reload the bundle
                notifyReload(reloadUrl, logger)
            } else if (!sidecarStarted) {
                // Fallback: start Node.js SSR sidecar on first build
                sidecarStarted = true
                logger.info(`  ${net('➜')}  ${net(colors.bold('SSR'))}:   ${colors.dim(`node --watch ${script}`)}`)

                ssrProcess = spawn('node', ['--watch', script], {
                    cwd: process.cwd(),
                    stdio: ['ignore', 'pipe', 'pipe'],
                    env: process.env,
                })
                pipeOutput(ssrProcess, '[ssr]', logger)
            }
        }
    })

    ssrBuildProcess.stderr?.on('data', (data: Buffer) => {
        const text = data.toString().trim()
        if (text) logger.warn(`  ${colors.dim('[ssr:build]')} ${text}`)
    })
}

/**
 * POST to the .NET reload endpoint to signal V8 engine hot-swap.
 * Retries if the .NET app isn't ready yet (common on first build).
 */
function notifyReload(url: string, logger: import('vite').Logger, retries = 5): void {
    fetch(url, { method: 'POST' })
        .then(res => {
            if (res.ok) {
                logger.info(`  ${colors.green('✓')}  ${colors.dim('[ssr]')} ${colors.green('V8 engines reloaded')}`)
            } else {
                logger.warn(`  ${colors.dim('[ssr]')} Reload signal failed: ${res.status}`)
            }
        })
        .catch(() => {
            if (retries > 0) {
                setTimeout(() => notifyReload(url, logger, retries - 1), 2000)
            } else {
                logger.warn(`  ${colors.dim('[ssr]')} Reload signal failed: .NET app not reachable`)
            }
        })
}

/**
 * Pipe child process output to the Vite logger.
 */
function pipeOutput(proc: ChildProcess, prefix: string, logger: import('vite').Logger): void {
    proc.stdout?.on('data', (data: Buffer) => {
        for (const line of data.toString().trim().split('\n')) {
            if (line.trim()) logger.info(`  ${colors.dim(prefix)} ${line.trim()}`)
        }
    })

    proc.stderr?.on('data', (data: Buffer) => {
        for (const line of data.toString().trim().split('\n')) {
            if (line.trim()) logger.warn(`  ${colors.dim(prefix)} ${line.trim()}`)
        }
    })

    proc.on('exit', (code) => {
        if (code !== null && code !== 0) {
            logger.error(`  ${colors.red(prefix)} Exited with code ${code}`)
        }
    })
}

/**
 * Stop SSR dev server if launched by us.
 */
function stopSsrSidecar(): void {
    if (ssrBuildProcess) { ssrBuildProcess.kill('SIGTERM'); ssrBuildProcess = null }
    if (ssrProcess) { ssrProcess.kill('SIGTERM'); ssrProcess = null }
}

/**
 * Stop the .NET server if it was launched by us.
 */
export function stopDotnetServer(): void {
    if (!dotnetProcess) return

    dotnetProcess.kill('SIGTERM')
    dotnetProcess = null
}

/**
 * Bind exit handlers to stop all managed processes on Vite shutdown.
 */
export function bindDotnetExitHandlers(): void {
    const cleanup = () => {
        stopDotnetServer()
        stopSsrSidecar()
    }

    process.on('exit', cleanup)
    process.on('SIGINT', () => { cleanup(); process.exit() })
    process.on('SIGTERM', () => { cleanup(); process.exit() })
}

