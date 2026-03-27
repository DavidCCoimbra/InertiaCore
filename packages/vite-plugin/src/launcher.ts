import { spawn, type ChildProcess } from 'child_process'
import { createConnection } from 'net'
import colors from 'picocolors'
import type { LauncherConfig } from './config.js'

let dotnetProcess: ChildProcess | null = null

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
 * Stop the .NET server if it was launched by us.
 */
export function stopDotnetServer(): void {
    if (!dotnetProcess) return

    dotnetProcess.kill('SIGTERM')
    dotnetProcess = null
}

/**
 * Bind exit handlers to stop the .NET server on Vite shutdown.
 */
export function bindDotnetExitHandlers(): void {
    const cleanup = () => stopDotnetServer()

    process.on('exit', cleanup)
    process.on('SIGINT', () => { cleanup(); process.exit() })
    process.on('SIGTERM', () => { cleanup(); process.exit() })
}

