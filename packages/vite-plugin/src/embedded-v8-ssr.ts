import type { Plugin } from 'vite';

/**
 * Vite plugin that wraps the SSR bundle for embedded V8 compatibility.
 * Appends a global function registration so ClearScript can call the render function.
 *
 * The developer's SSR entry file is unchanged — this transform is automatic.
 * The bundle works in both Node.js (standard server) and embedded V8.
 *
 * @example
 * ```ts
 * // vite.config.ts
 * import { embeddedV8Ssr } from '@inertiacore/vite/embedded-v8-ssr';
 *
 * export default defineConfig({
 *     plugins: [embeddedV8Ssr(), vue()],
 * });
 * ```
 */
export function embeddedV8Ssr(): Plugin {
    let isSsr = false;

    return {
        name: 'inertiacore-embedded-v8-ssr',
        apply: 'build',

        config(_config, env) {
            isSsr = !!env.isSsrBuild;
        },

        generateBundle(_options, bundle) {
            if (!isSsr) return;

            for (const [, chunk] of Object.entries(bundle)) {
                if (chunk.type === 'chunk' && chunk.isEntry) {
                    // Append global registration so embedded V8 can call the render function.
                    chunk.code += `\n;(typeof globalThis!=='undefined'?globalThis:this).__inertia_ssr_render=` +
                        `typeof __vite_ssr_exports__!=='undefined'?__vite_ssr_exports__.default:` +
                        `typeof module!=='undefined'?module.exports.default:undefined;\n`;
                }
            }
        },
    };
}
