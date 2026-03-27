import fs from 'fs'
import path from 'path'
import { UserConfig, SSROptions, PluginOption } from 'vite'
import fullReload, { Config as FullReloadConfig } from 'vite-plugin-full-reload'

// Vite 8 renamed Rollup → Rolldown. Support both with a local type.
export type InputOption = Record<string, string> | string[] | string

export interface DotnetVitePluginConfig {
    /** The path or paths of the entry points to compile. */
    input: InputOption
    /** The application's public directory. @default 'wwwroot' */
    publicDirectory?: string
    /** The public subdirectory where compiled assets should be written. @default 'build' */
    buildDirectory?: string
    /** The path to the "hot" file. @default `${publicDirectory}/hot` */
    hotFile?: string
    /** The path of the SSR entry point. */
    ssr?: InputOption
    /** The directory where the SSR bundle should be written. @default 'dist/ssr' */
    ssrOutputDirectory?: string
    /** Configuration for performing full page refresh on Razor/cshtml file changes. @default false */
    refresh?: boolean | string | string[] | RefreshConfig | RefreshConfig[]
    /** Transform the code while serving in development. */
    transformOnServe?: (code: string, url: DevServerUrl) => string
    /** Proxy API requests to the .NET server during development. @default true */
    proxy?: boolean | ProxyConfig
    /** Auto-start `dotnet run` when Vite dev server starts. @default true */
    launcher?: boolean | LauncherConfig
    /** Proxy WebSocket connections for SignalR hubs. @default true */
    signalR?: boolean | SignalRConfig
}

export interface ProxyConfig {
    /** URL patterns to proxy to .NET. @default ['/api'] */
    paths?: string[]
}

export interface LauncherConfig {
    /** Command to run. @default 'dotnet run' */
    command?: string
    /** Working directory. @default process.cwd() */
    cwd?: string
    /** Additional arguments. */
    args?: string[]
}

export interface SignalRConfig {
    /** SignalR hub path to proxy. @default '/inertia-hub' */
    hubPath?: string
}

export interface RefreshConfig {
    paths: string[]
    config?: FullReloadConfig
}

export type DevServerUrl = `${'http' | 'https'}://${string}:${number}`

export const refreshPaths = [
    'Pages/**/*.cshtml',
    'Views/**/*.cshtml',
    'Components/**/*.razor',
].filter(p => fs.existsSync(p.split('/')[0]))

/**
 * Convert the users configuration into a standard structure with defaults.
 */
export function resolveDotnetVitePluginConfig(config: string | string[] | DotnetVitePluginConfig): Required<DotnetVitePluginConfig> {
    if (typeof config === 'undefined') {
        throw new Error('@inertiacore/vite: missing configuration.')
    }

    if (typeof config === 'string' || Array.isArray(config)) {
        config = { input: config, ssr: config }
    }

    if (typeof config.input === 'undefined') {
        throw new Error('@inertiacore/vite: missing configuration for "input".')
    }

    if (typeof config.publicDirectory === 'string') {
        config.publicDirectory = config.publicDirectory.trim().replace(/^\/+/, '')
        if (config.publicDirectory === '') {
            throw new Error('@inertiacore/vite: publicDirectory must be a subdirectory. E.g. \'wwwroot\'.')
        }
    }

    if (typeof config.buildDirectory === 'string') {
        config.buildDirectory = config.buildDirectory.trim().replace(/^\/+/, '').replace(/\/+$/, '')
        if (config.buildDirectory === '') {
            throw new Error('@inertiacore/vite: buildDirectory must be a subdirectory. E.g. \'build\'.')
        }
    }

    if (typeof config.ssrOutputDirectory === 'string') {
        config.ssrOutputDirectory = config.ssrOutputDirectory.trim().replace(/^\/+/, '').replace(/\/+$/, '')
    }

    if (config.refresh === true) {
        config.refresh = [{ paths: refreshPaths }]
    }

    return {
        input: config.input,
        publicDirectory: config.publicDirectory ?? 'wwwroot',
        buildDirectory: config.buildDirectory ?? 'build',
        ssr: config.ssr ?? config.input,
        ssrOutputDirectory: config.ssrOutputDirectory ?? 'dist/ssr',
        refresh: config.refresh ?? false,
        hotFile: config.hotFile ?? path.join((config.publicDirectory ?? 'wwwroot'), 'hot'),
        transformOnServe: config.transformOnServe ?? ((code) => code),
        proxy: config.proxy ?? true,
        launcher: config.launcher ?? true,
        signalR: config.signalR ?? true,
    }
}

/**
 * Ensure the Vite dev server should run in the current environment.
 */
export function ensureCommandShouldRunInEnvironment(command: string, env: Record<string, string>): void {
    if (command === 'build' || env.ASPNETCORE_BYPASS_ENV_CHECK === '1') {
        return
    }

    if (typeof env.CI !== 'undefined') {
        throw new Error(
            '@inertiacore/vite: You should not run the Vite HMR server in CI environments. ' +
            'Set ASPNETCORE_BYPASS_ENV_CHECK=1 to override.'
        )
    }
}

/** Resolve the Vite base option from the configuration. */
export function resolveBase(config: Required<DotnetVitePluginConfig>, assetUrl: string): string {
    return assetUrl + (!assetUrl.endsWith('/') ? '/' : '') + config.buildDirectory + '/'
}

/** Resolve the Vite input path from the configuration. */
export function resolveInput(config: Required<DotnetVitePluginConfig>, ssr: boolean): InputOption | undefined {
    return ssr ? config.ssr : config.input
}

/** Resolve the Vite outDir path from the configuration. */
export function resolveOutDir(config: Required<DotnetVitePluginConfig>, ssr: boolean): string | undefined {
    return ssr ? config.ssrOutputDirectory : path.join(config.publicDirectory, config.buildDirectory)
}

/** Resolve the Vite build configuration. */
export function resolveBuildConfig(pluginConfig: Required<DotnetVitePluginConfig>, userConfig: UserConfig, ssr: boolean) {
    return {
        manifest: userConfig.build?.manifest ?? (ssr ? false : 'manifest.json'),
        ssrManifest: userConfig.build?.ssrManifest ?? (ssr ? 'ssr-manifest.json' : false),
        outDir: userConfig.build?.outDir ?? resolveOutDir(pluginConfig, ssr),
        rolldownOptions: {
            input: (userConfig.build as any)?.rolldownOptions?.input
                ?? userConfig.build?.rollupOptions?.input
                ?? resolveInput(pluginConfig, ssr)
        },
        assetsInlineLimit: userConfig.build?.assetsInlineLimit ?? 0,
    }
}

const defaultAliases: Record<string, string> = {
    '@': '/resources/js',
}

/** Resolve the path aliases, merging with user-defined aliases. */
export function resolveAliases(userConfig: UserConfig) {
    if (Array.isArray(userConfig.resolve?.alias)) {
        return [
            ...userConfig.resolve?.alias ?? [],
            ...Object.keys(defaultAliases).map(alias => ({
                find: alias,
                replacement: defaultAliases[alias]
            }))
        ]
    }

    return {
        ...defaultAliases,
        ...userConfig.resolve?.alias,
    }
}

/** Prevent Inertia helpers from being externalized in SSR builds. */
export function noExternalInertiaHelpers(config: UserConfig): true | Array<string | RegExp> {
    /* @ts-ignore */
    const userNoExternal = (config.ssr as SSROptions | undefined)?.noExternal
    const pluginNoExternal = ['@inertiacore/vite']

    if (userNoExternal === true) return true
    if (typeof userNoExternal === 'undefined') return pluginNoExternal

    return [
        ...(Array.isArray(userNoExternal) ? userNoExternal : [userNoExternal]),
        ...pluginNoExternal,
    ]
}

/** Resolve full reload plugins from config. */
export function resolveFullReloadConfig({ refresh: config }: Required<DotnetVitePluginConfig>): PluginOption[] {
    if (typeof config === 'boolean') return []
    if (typeof config === 'string') config = [{ paths: [config] }]
    if (!Array.isArray(config)) config = [config]
    if (config.some(c => typeof c === 'string')) config = [{ paths: config }] as RefreshConfig[]

    return (config as RefreshConfig[]).flatMap(c => {
        const plugin = fullReload(c.paths, c.config)
        /** @ts-ignore */
        plugin.__dotnet_plugin_config = c
        return plugin
    })
}
