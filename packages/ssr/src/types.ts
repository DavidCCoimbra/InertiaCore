/** The page object sent from ASP.NET to the SSR server. */
export interface Page {
    component: string;
    props: Record<string, unknown>;
    url: string;
    version: string | null;
    [key: string]: unknown;
}

/** The rendered result returned by the SSR render function. */
export interface SsrResponse {
    head: string[];
    body: string;
}

/** A function that renders a page object into HTML. */
export type RenderFunction = (page: Page) => Promise<SsrResponse>;

/** Options for creating an SSR server. */
export interface ServerOptions {
    /** Unix Domain Socket path (for MessagePack transport). Default: /tmp/inertia-ssr.sock */
    socketPath?: string;
}
