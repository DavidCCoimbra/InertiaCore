import fs from 'fs'
import path from 'path'
import colors from 'picocolors'
import { loadEnv, createLogger } from 'vite'

const logger = createLogger('info', {
    prefix: '[@inertiacore/vite]'
})

/**
 * Resolve the .NET application URL from multiple sources (in priority order):
 * 1. APP_URL from .env file
 * 2. ASPNETCORE_URLS environment variable
 * 3. launchSettings.json applicationUrl
 */
export function resolveAppUrl(envDir: string, mode: string): string | undefined {
    const env = loadEnv(mode, envDir, '')

    if (env.APP_URL) return env.APP_URL
    if (process.env.ASPNETCORE_URLS) return process.env.ASPNETCORE_URLS.split(';')[0]

    return resolveUrlFromLaunchSettings()
}

/**
 * Read the application URL from Properties/launchSettings.json.
 */
function resolveUrlFromLaunchSettings(): string | undefined {
    const launchSettingsPaths = [
        'Properties/launchSettings.json',
        '../Properties/launchSettings.json',
    ]

    for (const settingsPath of launchSettingsPaths) {
        try {
            if (!fs.existsSync(settingsPath)) continue

            const settings = JSON.parse(fs.readFileSync(settingsPath, 'utf-8'))
            const profiles = settings.profiles ?? {}

            for (const profile of Object.values(profiles) as any[]) {
                const urls = profile.applicationUrl as string | undefined
                if (!urls) continue

                const urlList = urls.split(';').map((u: string) => u.trim())
                return urlList.find((u: string) => u.startsWith('https://')) ?? urlList[0]
            }
        } catch {
            // Invalid JSON or missing file
        }
    }

    return undefined
}

/**
 * Detect if running under `dotnet watch` and log a warning.
 */
export function detectDotnetWatch(server: import('vite').ViteDevServer): void {
    if (process.env.DOTNET_WATCH === '1') {
        server.config.logger.info(
            `  ${colors.yellow('⚠')}  ${colors.dim('dotnet watch detected — .NET server may restart on a different port')}`
        )
    }
}

/**
 * Auto-detect ASP.NET Core HTTPS dev certificate.
 * Looks for PEM+key in standard locations per OS.
 */
export function resolveHttpsDevCert(): { cert: string; key: string } | undefined {
    const home = process.env.HOME ?? process.env.USERPROFILE ?? ''

    const certDirs = [
        path.join(home, '.aspnet', 'https'),                          // macOS / Linux
        path.join(process.env.APPDATA ?? '', 'ASP.NET', 'https'),     // Windows
    ]

    for (const certDir of certDirs) {
        if (!fs.existsSync(certDir)) continue

        try {
            const files = fs.readdirSync(certDir)
            const pem = files.find(f => f.endsWith('.pem'))
            const key = files.find(f => f.endsWith('.key'))
            const pfx = files.find(f => f.endsWith('.pfx'))

            if (pem && key) {
                return {
                    cert: path.join(certDir, pem),
                    key: path.join(certDir, key),
                }
            }

            if (pfx && !pem) {
                logger.info(
                    `  ${colors.dim('HTTPS dev cert found as .pfx — export as PEM for Vite:')} ` +
                    `${colors.cyan(`dotnet dev-certs https --export-path ${path.join(certDir, 'dev.pem')} --format Pem`)}`
                )
            }
        } catch {
            // Permission error
        }
    }

    return undefined
}
