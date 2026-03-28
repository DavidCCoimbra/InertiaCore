import fs from 'fs'
import { Plugin, loadEnv, UserConfig, ConfigEnv, ResolvedConfig } from 'vite'

import {
    type DotnetVitePluginConfig,
    type DevServerUrl,
    resolveDotnetVitePluginConfig,
    ensureCommandShouldRunInEnvironment,
    resolveBase,
    resolveBuildConfig,
    resolveAliases,
    noExternalInertiaHelpers,
    resolveFullReloadConfig,
} from './config.js'

import {
    resolveServerConfig,
    resolveListeningServerUrl,
    writeHotFile,
    bindExitHandlers,
    logServerStart,
} from './server.js'

import { resolveAppUrl } from './dotnet.js'
import { startDotnetServer, startSsrSidecar, bindDotnetExitHandlers } from './launcher.js'

// Re-export types and utilities for external use
export type { DotnetVitePluginConfig, RefreshConfig, DevServerUrl, ProxyConfig, LauncherConfig, SignalRConfig, SsrDevConfig } from './config.js'
export { refreshPaths } from './config.js'
export { writeHotFile, bindExitHandlers } from './server.js'

interface DotnetPlugin extends Plugin {
    config: (config: UserConfig, env: ConfigEnv) => UserConfig
}

/**
 * ASP.NET Core plugin for Vite.
 *
 * @param config - A config object or relative path(s) of the scripts to be compiled.
 */
export default function dotnetVite(config: string | string[] | DotnetVitePluginConfig): Plugin[] {
    const pluginConfig = resolveDotnetVitePluginConfig(config)
    const ssrDevOpts = typeof pluginConfig.ssrDev === 'object' ? pluginConfig.ssrDev : {}
    const isV8 = !!ssrDevOpts.v8

    return [
        // V8 resolve/load hooks must run BEFORE other plugins resolve imports
        ...(isV8 ? [resolveV8Plugin()] : []),
        resolveDotnetPlugin(pluginConfig),
        ...resolveFullReloadConfig(pluginConfig) as Plugin[],
    ]
}

/**
 * Separate pre-enforce plugin for V8 SSR import interception.
 * Must run before Rolldown resolves @inertiajs/server imports.
 */
function resolveV8Plugin(): Plugin {
    return {
        name: '@inertiacore/vite:v8',
        enforce: 'pre',
        resolveId(id) {
            if (/^@inertiajs\/[\w-]+\/(server|dist\/server)(\.js)?$/.test(id)) {
                return '\0inertiacore-v8-server-mock'
            }
            return null
        },
        load(id) {
            if (id === '\0inertiacore-v8-server-mock') {
                return `export default function createServer(renderCallback, port) {
                    globalThis.__inertia_ssr_render = renderCallback;
                }`
            }
            return null
        },
    }
}

/**
 * Resolve the core ASP.NET Core Vite plugin with all hooks.
 */
function resolveDotnetPlugin(pluginConfig: Required<DotnetVitePluginConfig>): DotnetPlugin {
    let viteDevServerUrl: DevServerUrl
    let resolvedConfig: ResolvedConfig
    let userConfig: UserConfig

    // Resolve V8 mode from ssrDev config
    const ssrDevOpts = typeof pluginConfig.ssrDev === 'object' ? pluginConfig.ssrDev : {}
    const isV8 = !!ssrDevOpts.v8

    return {
        name: '@inertiacore/vite',
        enforce: 'post',
        config: (config, { command, mode }) => {
            userConfig = config
            const ssr = !!userConfig.build?.ssr
            const env = loadEnv(mode, userConfig.envDir || process.cwd(), '')
            const assetUrl = env.ASSET_URL ?? ''
            const isV8SsrBuild = ssr && isV8

            ensureCommandShouldRunInEnvironment(command, env)

            const userAliases = resolveAliases(userConfig)
            const aliases = userAliases

            const result: Record<string, unknown> = {
                base: userConfig.base ?? (command === 'build' ? resolveBase(pluginConfig, assetUrl) : ''),
                publicDir: userConfig.publicDir ?? false,
                build: resolveBuildConfig(pluginConfig, userConfig, ssr),
                server: resolveServerConfig(userConfig, env, pluginConfig.proxy, pluginConfig.signalR),
                resolve: { alias: aliases },
                ssr: { noExternal: isV8SsrBuild ? true : noExternalInertiaHelpers(userConfig) },
            }

            // EmbeddedV8: self-contained IIFE bundle for ClearScript V8
            if (isV8SsrBuild) {
                const buildConfig = result.build as Record<string, unknown>
                const rolldownOpts = (buildConfig.rolldownOptions ?? {}) as Record<string, unknown>
                rolldownOpts.output = { ...(rolldownOpts.output as Record<string, unknown> ?? {}), format: 'iife' }
                buildConfig.rolldownOptions = rolldownOpts
                result.build = buildConfig
                result.ssr = { noExternal: true }
            }

            return result
        },
        configResolved(config) {
            resolvedConfig = config
        },
        transform(code, id) {

            // Dev server: replace placeholder URLs
            if (resolvedConfig?.command === 'serve') {
                code = code.replace(/__aspnetcore_vite_placeholder__/g, viteDevServerUrl)
                return pluginConfig.transformOnServe(code, viteDevServerUrl)
            }
        },
        configureServer(server) {
            const envDir = resolvedConfig?.envDir || process.cwd()

            const appUrl = resolveAppUrl(envDir, resolvedConfig?.mode ?? 'development')

            // Skip launcher/SSR hooks when running inside the SSR dev server's Vite instance
            // (createDevServer() creates its own Vite — without this guard, infinite recursion)
            const isInsideSsrDev = process.env.INERTIACORE_SSR_DEV === '1'

            if (!isInsideSsrDev) {
                // Launch .NET server if configured (detects port conflicts)
                if (pluginConfig.launcher !== false) {
                    startDotnetServer(pluginConfig.launcher, server.config.logger, appUrl)
                }

                // Launch SSR sidecar if configured
                if (pluginConfig.ssrDev !== false) {
                    startSsrSidecar(pluginConfig.ssrDev, server.config.logger)
                }

                bindDotnetExitHandlers()
            }

            server.httpServer?.once('listening', () => {
                viteDevServerUrl = resolveListeningServerUrl(server, userConfig)
                writeHotFile(pluginConfig.hotFile, viteDevServerUrl, server.config.base)
                logServerStart(server, appUrl, `${viteDevServerUrl}${server.config.base}`)
            })

            bindExitHandlers(pluginConfig.hotFile)

            return () => server.middlewares.use((req, res, next) => {
                if (req.url === '/index.html') {
                    const appUrl = resolveAppUrl(envDir, resolvedConfig?.mode ?? 'development')

                    res.statusCode = 404
                    res.end(
                        fs.readFileSync(new URL('./dev-server-index.html', import.meta.url), 'utf-8')
                            .replace(/\{\{ APP_URL \}\}/g, appUrl ?? 'http://localhost:5000')
                    )
                }
                next()
            })
        }
    }
}
