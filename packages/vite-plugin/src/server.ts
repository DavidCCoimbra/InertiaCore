import fs from 'fs'
import { AddressInfo } from 'net'
import { fileURLToPath } from 'url'
import path from 'path'
import colors from 'picocolors'
import { UserConfig, ResolvedConfig, defaultAllowedOrigins, createLogger } from 'vite'
import type { DevServerUrl, ProxyConfig, SignalRConfig } from './config.js'
import { resolveHttpsDevCert, detectDotnetWatch, resolveAppUrl } from './dotnet.js'

const logger = createLogger('info', {
    prefix: '[@inertiacore/vite]'
})

let exitHandlersBound = false

/**
 * Resolve the Vite server configuration with CORS and optional HTTPS.
 */
export function resolveServerConfig(
    userConfig: UserConfig,
    env: Record<string, string>,
    proxyConfig?: boolean | ProxyConfig,
    signalRConfig?: boolean | SignalRConfig,
) {
    const httpsCert = userConfig.server?.https ? undefined : resolveHttpsDevCert()
    const appUrl = env.APP_URL ?? process.env.ASPNETCORE_URLS?.split(';')[0]

    return {
        origin: userConfig.server?.origin ?? '__aspnetcore_vite_placeholder__',
        ...(httpsCert ? {
            https: { cert: httpsCert.cert, key: httpsCert.key },
        } : undefined),
        cors: userConfig.server?.cors ?? {
            origin: userConfig.server?.origin ?? [
                defaultAllowedOrigins,
                ...(env.APP_URL ? [env.APP_URL] : []),
                /^https?:\/\/localhost(:\d+)?$/,
                /^https?:\/\/127\.0\.0\.1(:\d+)?$/,
            ],
        },
        proxy: userConfig.server?.proxy ?? resolveProxyConfig(appUrl, proxyConfig, signalRConfig),
    }
}

/**
 * Build Vite proxy configuration for .NET API and SignalR.
 */
function resolveProxyConfig(
    appUrl: string | undefined,
    proxyConfig?: boolean | ProxyConfig,
    signalRConfig?: boolean | SignalRConfig,
): Record<string, any> | undefined {
    if (proxyConfig === false && signalRConfig === false) return undefined
    if (!appUrl) return undefined

    const target = appUrl
    const proxy: Record<string, any> = {}

    // API proxy
    if (proxyConfig !== false) {
        const paths = (typeof proxyConfig === 'object' ? proxyConfig.paths : undefined) ?? ['/api']

        for (const path of paths) {
            proxy[path] = {
                target,
                changeOrigin: true,
                secure: false,
            }
        }
    }

    // SignalR WebSocket proxy
    if (signalRConfig !== false) {
        const hubPath = (typeof signalRConfig === 'object' ? signalRConfig.hubPath : undefined) ?? '/inertia-hub'

        proxy[hubPath] = {
            target,
            changeOrigin: true,
            secure: false,
            ws: true,
        }
    }

    return Object.keys(proxy).length > 0 ? proxy : undefined
}

/**
 * Resolve the dev server URL once the server is listening.
 */
export function resolveListeningServerUrl(server: import('vite').ViteDevServer, userConfig: UserConfig): DevServerUrl {
    const address = server.httpServer?.address()

    const isAddressInfo = (x: string | AddressInfo | null | undefined): x is AddressInfo => typeof x === 'object'
    if (isAddressInfo(address)) {
        return userConfig.server?.origin
            ? userConfig.server.origin as DevServerUrl
            : resolveDevServerUrl(address, server.config, userConfig)
    }

    return 'http://localhost:5173' as DevServerUrl
}

/**
 * Write the hot file so the .NET server can discover the Vite dev server.
 * @internal Exported for testing.
 */
export function writeHotFile(hotFile: string, devServerUrl: DevServerUrl, base: string): void {
    const hotFileParentDirectory = path.dirname(hotFile)

    if (!fs.existsSync(hotFileParentDirectory)) {
        fs.mkdirSync(hotFileParentDirectory, { recursive: true })

        setTimeout(() => {
            logger.info(`Hot file directory created ${colors.dim(fs.realpathSync(hotFileParentDirectory))}`, { clear: true, timestamp: true })
        }, 200)
    }

    fs.writeFileSync(hotFile, `${devServerUrl}${base.replace(/\/$/, '')}`)
}

/**
 * Bind process exit handlers to clean up the hot file.
 * @internal Exported for testing.
 */
export function bindExitHandlers(hotFile: string): void {
    if (exitHandlersBound) return

    const clean = () => {
        if (fs.existsSync(hotFile)) fs.rmSync(hotFile)
    }

    process.on('exit', clean)
    process.on('SIGINT', () => process.exit())
    process.on('SIGTERM', () => process.exit())
    process.on('SIGHUP', () => process.exit())

    exitHandlersBound = true
}

/**
 * Log the plugin banner on server start. Pings the .NET server to show live status.
 */
export function logServerStart(server: import('vite').ViteDevServer, appUrl?: string, viteUrl?: string): void {
    setTimeout(async () => {
        const log = server.config.logger
        const net = (s: string) => `\x1b[38;2;204;110;212m${s}\x1b[0m`
        const version = pluginVersion()
        const env = resolveEnvironment()

        log.info('')
        log.info(`  ${net(colors.bold('ASP.NET Core'))}  ${colors.dim('plugin')} ${version ? colors.bold(`v${version}`) : ''}  ${formatEnvironment(env)}`)

        if (viteUrl) {
            log.info(`  ${net('➜')}  ${net(colors.bold('Vite'))}:  ${colors.cyan(viteUrl)}`)
        }

        if (appUrl) {
            const status = await pingServer(appUrl)
            const statusIcon = status ? colors.green('✓ running') : colors.yellow('⏳ waiting')
            log.info(`  ${net('➜')}  ${net(colors.bold('App'))}:   ${colors.cyan(appUrl)}  ${statusIcon}`)
        }

        detectDotnetWatch(server)
        log.info('')

        if (appUrl) pollForServer(server, appUrl)
    }, 100)
}

/**
 * Resolve the current ASP.NET Core environment.
 */
function resolveEnvironment(): string {
    return process.env.ASPNETCORE_ENVIRONMENT ?? process.env.DOTNET_ENVIRONMENT ?? 'Development'
}

/**
 * Format the environment name with color.
 */
function formatEnvironment(env: string): string {
    switch (env.toLowerCase()) {
        case 'development': return colors.green(env)
        case 'staging': return colors.yellow(env)
        case 'production': return colors.red(colors.bold(env))
        default: return colors.cyan(env)
    }
}

/** Ping the .NET server to check if it's running. */
async function pingServer(url: string): Promise<boolean> {
    try {
        const controller = new AbortController()
        const timeout = setTimeout(() => controller.abort(), 2000)
        await fetch(url, { signal: controller.signal }).catch(() => null)
        clearTimeout(timeout)
        return true
    } catch {
        return false
    }
}

/** Poll for the .NET server to come online, log when it does, and open browser. */
function pollForServer(server: import('vite').ViteDevServer, appUrl: string): void {
    let attempts = 0
    const maxAttempts = 60

    const interval = setInterval(async () => {
        attempts++
        if (attempts > maxAttempts) { clearInterval(interval); return }

        const running = await pingServer(appUrl)
        if (running) {
            clearInterval(interval)
            const net = (s: string) => `\x1b[38;2;204;110;212m${s}\x1b[0m`
            server.config.logger.info(
                `  ${net('➜')}  ${net(colors.bold('App'))}:   ${colors.cyan(appUrl)}  ${colors.green('✓ running')}`
            )
        }
    }, 1000)
}

/** Resolve the dev server URL from the server address and configuration. */
function resolveDevServerUrl(address: AddressInfo, config: ResolvedConfig, userConfig: UserConfig): DevServerUrl {
    const configHmrProtocol = typeof config.server.hmr === 'object' ? config.server.hmr.protocol : null
    const clientProtocol = configHmrProtocol ? (configHmrProtocol === 'wss' ? 'https' : 'http') : null
    const serverProtocol = config.server.https ? 'https' : 'http'
    const protocol = clientProtocol ?? serverProtocol

    const configHmrHost = typeof config.server.hmr === 'object' ? config.server.hmr.host : null
    const configHost = typeof config.server.host === 'string' ? config.server.host : null
    const serverAddress = isIpv6(address) ? `[${address.address}]` : address.address
    const host = configHmrHost ?? configHost ?? serverAddress

    const configHmrClientPort = typeof config.server.hmr === 'object' ? config.server.hmr.clientPort : null
    const port = configHmrClientPort ?? address.port

    return `${protocol}://${host}:${port}`
}

function isIpv6(address: AddressInfo): boolean {
    return address.family === 'IPv6'
        // @ts-ignore-next-line
        || address.family === 6
}

/** The version of the ASP.NET Core Vite plugin being run. */
function pluginVersion(): string {
    try {
        return JSON.parse(fs.readFileSync(path.join(dirname(), '../package.json')).toString())?.version
    } catch {
        return ''
    }
}

function dirname(): string {
    return fileURLToPath(new URL('.', import.meta.url))
}
