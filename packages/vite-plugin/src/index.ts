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
import { startDotnetServer, bindDotnetExitHandlers } from './launcher.js'

// Re-export types and utilities for external use
export type { DotnetVitePluginConfig, RefreshConfig, DevServerUrl, ProxyConfig, LauncherConfig, SignalRConfig } from './config.js'
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
export default function dotnetVite(config: string | string[] | DotnetVitePluginConfig): [DotnetPlugin, ...Plugin[]] {
    const pluginConfig = resolveDotnetVitePluginConfig(config)

    return [
        resolveDotnetPlugin(pluginConfig),
        ...resolveFullReloadConfig(pluginConfig) as Plugin[],
    ]
}

/**
 * Resolve the core ASP.NET Core Vite plugin with all hooks.
 */
function resolveDotnetPlugin(pluginConfig: Required<DotnetVitePluginConfig>): DotnetPlugin {
    let viteDevServerUrl: DevServerUrl
    let resolvedConfig: ResolvedConfig
    let userConfig: UserConfig

    return {
        name: '@inertiacore/vite',
        enforce: 'post',
        config: (config, { command, mode }) => {
            userConfig = config
            const ssr = !!userConfig.build?.ssr
            const env = loadEnv(mode, userConfig.envDir || process.cwd(), '')
            const assetUrl = env.ASSET_URL ?? ''

            ensureCommandShouldRunInEnvironment(command, env)

            return {
                base: userConfig.base ?? (command === 'build' ? resolveBase(pluginConfig, assetUrl) : ''),
                publicDir: userConfig.publicDir ?? false,
                build: resolveBuildConfig(pluginConfig, userConfig, ssr),
                server: resolveServerConfig(userConfig, env, pluginConfig.proxy, pluginConfig.signalR),
                resolve: { alias: resolveAliases(userConfig) },
                ssr: { noExternal: noExternalInertiaHelpers(userConfig) },
            }
        },
        configResolved(config) {
            resolvedConfig = config
        },
        transform(code) {
            if (resolvedConfig.command === 'serve') {
                code = code.replace(/__aspnetcore_vite_placeholder__/g, viteDevServerUrl)
                return pluginConfig.transformOnServe(code, viteDevServerUrl)
            }
        },
        configureServer(server) {
            const envDir = resolvedConfig?.envDir || process.cwd()

            const appUrl = resolveAppUrl(envDir, resolvedConfig?.mode ?? 'development')

            // Launch .NET server if configured (detects port conflicts)
            if (pluginConfig.launcher !== false) {
                startDotnetServer(pluginConfig.launcher, server.config.logger, appUrl)
                bindDotnetExitHandlers()
            }

            server.httpServer?.once('listening', () => {
                viteDevServerUrl = resolveListeningServerUrl(server, userConfig)
                writeHotFile(pluginConfig.hotFile, viteDevServerUrl, server.config.base)
                logServerStart(server, appUrl)
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
