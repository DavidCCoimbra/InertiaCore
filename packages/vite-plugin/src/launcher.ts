import { spawn, type ChildProcess } from 'child_process'
import { createConnection } from 'net'
import colors from 'picocolors'
import type { LauncherConfig, SsrDevConfig } from './config.js'

let dotnetProcess: ChildProcess | null = null
let ssrBuildProcess: ChildProcess | null = null
let ssrProcess: ChildProcess | null = null

// Build error pattern: CS followed by 4 digits
const buildErrorPattern = /\b(CS\d{4}|error\s+CS\d{4})/

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

    const net = (s: string) => `\x1b[38;2;204;110;212m${s}\x1b[0m`

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
 * Start SSR development mode:
 * 1. `vite build --ssr --watch` — continuously rebuilds SSR bundle on file changes
 * 2. `node --watch dist/ssr/ssr.js` — auto-restarts sidecar when bundle changes
 *
 * Flow: edit .vue file → Vite rebuilds SSR bundle → Node detects change → sidecar restarts → refresh to see SSR output
 */
export function startSsrSidecar(
    config: boolean | SsrDevConfig,
    logger: import('vite').Logger,
): void {
    if (config === false || ssrBuildProcess) return

    const opts: SsrDevConfig = typeof config === 'object' ? config : {}
    const script = opts.script ?? 'dist/ssr/ssr.js'
    const port = opts.port ?? 13714
    const net = (s: string) => `\x1b[38;2;204;110;212m${s}\x1b[0m`

    // Check if SSR sidecar is already running
    const portCheck = createConnection({ port, host: '127.0.0.1' })
    portCheck.on('connect', () => {
        portCheck.destroy()
        logger.info(`  ${net('➜')}  ${net(colors.bold('SSR'))}:   ${colors.cyan(`http://127.0.0.1:${port}`)}  ${colors.green('✓ already running')} ${colors.dim('(skipping)')}`)
    })
    portCheck.on('error', () => {
        portCheck.destroy()

        // Start watching SSR builder — rebuilds on every file change
        logger.info(`  ${net('➜')}  ${net(colors.bold('SSR'))}:   ${colors.dim('vite build --ssr --watch')}`)

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
                if (!line.trim()) continue
                logger.info(`  ${colors.dim('[ssr:build]')} ${line.trim()}`)
            }

            // Start sidecar after first successful build
            if (!sidecarStarted && (text.includes('built in') || text.includes('✓'))) {
                sidecarStarted = true
                startSsrNode(script, logger)
            }
        })

        ssrBuildProcess.stderr?.on('data', (data: Buffer) => {
            const text = data.toString().trim()
            if (text) logger.warn(`  ${colors.dim('[ssr:build]')} ${text}`)
        })

        ssrBuildProcess.on('exit', (code) => {
            if (code !== null && code !== 0) {
                logger.error(`  ${colors.red('[ssr:build]')} Exited with code ${code}`)
            }
            ssrBuildProcess = null
        })
    })
    portCheck.setTimeout(500, () => { portCheck.destroy() })
}

/**
 * Start the Node.js SSR sidecar with --watch (auto-restarts when bundle changes).
 */
function startSsrNode(script: string, logger: import('vite').Logger): void {
    const net = (s: string) => `\x1b[38;2;204;110;212m${s}\x1b[0m`
    logger.info(`  ${net('➜')}  ${net(colors.bold('SSR'))}:   ${colors.dim(`node --watch ${script}`)}`)

    ssrProcess = spawn('node', ['--watch', script], {
        cwd: process.cwd(),
        stdio: ['ignore', 'pipe', 'pipe'],
        env: process.env,
    })

    ssrProcess.stdout?.on('data', (data: Buffer) => {
        for (const line of data.toString().trim().split('\n')) {
            if (line.trim()) logger.info(`  ${colors.dim('[ssr]')} ${line.trim()}`)
        }
    })

    ssrProcess.stderr?.on('data', (data: Buffer) => {
        for (const line of data.toString().trim().split('\n')) {
            if (line.trim()) logger.warn(`  ${colors.dim('[ssr]')} ${line.trim()}`)
        }
    })

    ssrProcess.on('exit', (code) => {
        if (code !== null && code !== 0) {
            logger.error(`  ${colors.red('[ssr]')} Exited with code ${code}`)
        }
        ssrProcess = null
    })
}

/**
 * Stop SSR processes if launched by us.
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

