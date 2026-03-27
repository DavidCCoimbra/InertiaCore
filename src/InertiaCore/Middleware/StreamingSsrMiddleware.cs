using System.Text;
using System.Text.RegularExpressions;
using InertiaCore.Constants;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InertiaCore.Middleware;

/// <summary>
/// Middleware that streams the HTML response in three phases for faster TTFB.
/// Phase 1: Shell (head, CSS, layout) flushed immediately.
/// Phase 2: SSR-rendered content flushed after render completes.
/// Phase 3: Hydration data (page JSON + app JS) flushed last.
/// </summary>
public sealed partial class StreamingSsrMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StreamingSsrMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="StreamingSsrMiddleware"/>.
    /// </summary>
    public StreamingSsrMiddleware(RequestDelegate next, ILogger<StreamingSsrMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context)
    {
        // Only intercept initial page loads — XHR Inertia requests get JSON, not HTML
        if (context.Request.Headers.ContainsKey(InertiaHeaders.Inertia))
        {
            await _next(context);
            return;
        }

        // Capture the response to extract the page object
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await _next(context);

        // Only stream HTML responses (Inertia initial page loads)
        var contentType = context.Response.ContentType ?? "";
        if (!contentType.Contains("text/html"))
        {
            buffer.Seek(0, SeekOrigin.Begin);
            context.Response.Body = originalBody;
            await buffer.CopyToAsync(originalBody);
            return;
        }

        buffer.Seek(0, SeekOrigin.Begin);
        var html = await new StreamReader(buffer).ReadToEndAsync();

        // Extract page JSON from <script data-page>
        var pageJson = ExtractPageJson(html);
        if (pageJson is null)
        {
            // Not an Inertia page — send as-is
            context.Response.Body = originalBody;
            context.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
            await originalBody.WriteAsync(Encoding.UTF8.GetBytes(html));
            return;
        }

        // Stream the response in three phases
        context.Response.Body = originalBody;
        context.Response.ContentLength = null;
        context.Response.Headers["Transfer-Encoding"] = "chunked";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Response.Headers.CacheControl = "no-cache";

        var (shell, remainder) = SplitAtAppDiv(html);

        // PHASE 1: Send the shell immediately (head, CSS, layout)
        LogPhaseFlush(_logger, 1, "shell");
        await originalBody.WriteAsync(Encoding.UTF8.GetBytes(shell));
        await originalBody.FlushAsync();

        // PHASE 2: SSR content (already rendered in the captured response)
        // The SSR body is between <div id="app"> and the page script
        var ssrContent = ExtractSsrContent(html);
        if (ssrContent is not null)
        {
            LogPhaseFlush(_logger, 2, "ssr-content");
            await originalBody.WriteAsync(Encoding.UTF8.GetBytes(ssrContent));
            await originalBody.FlushAsync();
        }

        // PHASE 3: Hydration data + closing HTML
        LogPhaseFlush(_logger, 3, "hydration");
        await originalBody.WriteAsync(Encoding.UTF8.GetBytes(remainder));
        await originalBody.FlushAsync();
    }

    private static string? ExtractPageJson(string html)
    {
        var match = PageScriptPattern().Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static (string shell, string remainder) SplitAtAppDiv(string html)
    {
        const string AppDivMarker = "<div id=\"app\">";
        var idx = html.IndexOf(AppDivMarker, StringComparison.Ordinal);
        if (idx < 0)
        {
            return (html, "");
        }

        var shellEnd = idx + AppDivMarker.Length;
        return (html[..shellEnd], html[shellEnd..]);
    }

    private static string? ExtractSsrContent(string html)
    {
        const string StartMarker = "<div id=\"app\">";
        const string EndMarker = "<script data-page=";

        var start = html.IndexOf(StartMarker, StringComparison.Ordinal);
        if (start < 0) return null;
        start += StartMarker.Length;

        var end = html.IndexOf(EndMarker, start, StringComparison.Ordinal);
        if (end < 0) return null;

        var content = html[start..end].Trim();
        return content.Length > 0 ? content : null;
    }

    [GeneratedRegex(@"<script data-page=""[^""]*"" type=""application/json"">(.*?)</script>")]
    private static partial Regex PageScriptPattern();

    [LoggerMessage(Level = LogLevel.Debug, Message = "SSR streaming: phase {Phase} flushed ({Label})")]
    private static partial void LogPhaseFlush(ILogger logger, int phase, string label);
}
