using InertiaCore.Constants;
using InertiaCore.Contracts;
using InertiaCore.Core;
using InertiaCore.Props;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore;

/// <summary>
/// Static convenience helper for Inertia operations. Prop factory methods (Always, Defer, etc.)
/// are pure constructors and work without initialization. Request-scoped methods (Render, Share,
/// Flash, Location) require explicit opt-in via services.AddInertiaStaticHelper().
/// </summary>
public static class Inertia
{
    private static IHttpContextAccessor? s_httpContextAccessor;

    /// <summary>
    /// Initializes the static helper. Called by AddInertiaStaticHelper().
    /// </summary>
    internal static void Initialize(IHttpContextAccessor httpContextAccessor)
    {
        s_httpContextAccessor = httpContextAccessor;
    }

    // -- Request-scoped methods (require AddInertiaStaticHelper) --

    /// <summary>
    /// Renders an Inertia response for the given component.
    /// </summary>
    public static InertiaResponse Render(string component, Dictionary<string, object?>? props = null) =>
        GetFactory().Render(component, props);

    /// <summary>
    /// Renders an Inertia response with anonymous object props.
    /// </summary>
    public static InertiaResponse Render(string component, object props) =>
        GetFactory().Render(component, props);

    /// <summary>
    /// Renders an Inertia response with typed props.
    /// </summary>
    public static InertiaResponse Render<TProps>(string component, TProps props) where TProps : class =>
        GetFactory().Render(component, props);

    /// <summary>
    /// Shares a prop for this request.
    /// </summary>
    public static void Share(string key, object? value) =>
        GetFactory().Share(key, value);

    /// <summary>
    /// Stores flash data for the next response.
    /// </summary>
    public static void Flash(string key, object? value) =>
        GetFactory().Flash(key, value);

    /// <summary>
    /// Returns an external redirect response via 409 + X-Inertia-Location header.
    /// </summary>
    public static IResult Location(string url)
    {
        var context = GetHttpContext();
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.Headers[InertiaHeaders.Location] = url;
        return Results.StatusCode(StatusCodes.Status409Conflict);
    }

    // -- Prop factory methods (pure constructors, no initialization required) --

    // Always

    /// <inheritdoc cref="InertiaResponseFactory.Always(object?)"/>
    public static AlwaysProp Always(object? value) => InertiaResponseFactory.Always(value);

    /// <inheritdoc cref="InertiaResponseFactory.Always(object?)"/>
    public static AlwaysProp Always(Func<object?> callback) => InertiaResponseFactory.Always(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Always(object?)"/>
    public static AlwaysProp Always(Func<Task<object?>> callback) => InertiaResponseFactory.Always(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Always(object?)"/>
    public static AlwaysProp Always(Func<IServiceProvider, object?> callback) => InertiaResponseFactory.Always(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Always(object?)"/>
    public static AlwaysProp Always(Func<IServiceProvider, Task<object?>> callback) => InertiaResponseFactory.Always(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Always(object?)"/>
    public static AlwaysProp<T> Always<T>(T? value) => InertiaResponseFactory.Always(value);

    /// <inheritdoc cref="InertiaResponseFactory.Always(object?)"/>
    public static AlwaysProp<T> Always<T>(Func<T?> callback) => InertiaResponseFactory.Always(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Always(object?)"/>
    public static AlwaysProp<T> Always<T>(Func<Task<T?>> callback) => InertiaResponseFactory.Always(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Always(object?)"/>
    public static AlwaysProp<T> Always<T>(Func<IServiceProvider, T?> callback) => InertiaResponseFactory.Always(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Always(object?)"/>
    public static AlwaysProp<T> Always<T>(Func<IServiceProvider, Task<T?>> callback) => InertiaResponseFactory.Always(callback);

    // Optional

    /// <inheritdoc cref="InertiaResponseFactory.Optional(Func{object})"/>
    public static OptionalProp Optional(Func<object?> callback) => InertiaResponseFactory.Optional(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Optional(Func{object})"/>
    public static OptionalProp Optional(Func<Task<object?>> callback) => InertiaResponseFactory.Optional(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Optional(Func{object})"/>
    public static OptionalProp Optional(Func<IServiceProvider, object?> callback) => InertiaResponseFactory.Optional(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Optional(Func{object})"/>
    public static OptionalProp Optional(Func<IServiceProvider, Task<object?>> callback) => InertiaResponseFactory.Optional(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Optional(Func{object})"/>
    public static OptionalProp<T> Optional<T>(Func<T?> callback) => InertiaResponseFactory.Optional(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Optional(Func{object})"/>
    public static OptionalProp<T> Optional<T>(Func<Task<T?>> callback) => InertiaResponseFactory.Optional(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Optional(Func{object})"/>
    public static OptionalProp<T> Optional<T>(Func<IServiceProvider, T?> callback) => InertiaResponseFactory.Optional(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Optional(Func{object})"/>
    public static OptionalProp<T> Optional<T>(Func<IServiceProvider, Task<T?>> callback) => InertiaResponseFactory.Optional(callback);

    // Defer

    /// <inheritdoc cref="InertiaResponseFactory.Defer(Func{object}, string?)"/>
    public static DeferProp Defer(Func<object?> callback, string? group = null) => InertiaResponseFactory.Defer(callback, group);

    /// <inheritdoc cref="InertiaResponseFactory.Defer(Func{object}, string?)"/>
    public static DeferProp Defer(Func<Task<object?>> callback, string? group = null) => InertiaResponseFactory.Defer(callback, group);

    /// <inheritdoc cref="InertiaResponseFactory.Defer(Func{object}, string?)"/>
    public static DeferProp Defer(Func<IServiceProvider, object?> callback, string? group = null) => InertiaResponseFactory.Defer(callback, group);

    /// <inheritdoc cref="InertiaResponseFactory.Defer(Func{object}, string?)"/>
    public static DeferProp Defer(Func<IServiceProvider, Task<object?>> callback, string? group = null) => InertiaResponseFactory.Defer(callback, group);

    /// <inheritdoc cref="InertiaResponseFactory.Defer(Func{object}, string?)"/>
    public static DeferProp<T> Defer<T>(Func<T?> callback, string? group = null) => InertiaResponseFactory.Defer(callback, group);

    /// <inheritdoc cref="InertiaResponseFactory.Defer(Func{object}, string?)"/>
    public static DeferProp<T> Defer<T>(Func<Task<T?>> callback, string? group = null) => InertiaResponseFactory.Defer(callback, group);

    /// <inheritdoc cref="InertiaResponseFactory.Defer(Func{object}, string?)"/>
    public static DeferProp<T> Defer<T>(Func<IServiceProvider, T?> callback, string? group = null) => InertiaResponseFactory.Defer(callback, group);

    /// <inheritdoc cref="InertiaResponseFactory.Defer(Func{object}, string?)"/>
    public static DeferProp<T> Defer<T>(Func<IServiceProvider, Task<T?>> callback, string? group = null) => InertiaResponseFactory.Defer(callback, group);

    // Merge

    /// <inheritdoc cref="InertiaResponseFactory.Merge(object?)"/>
    public static MergeProp Merge(object? value) => InertiaResponseFactory.Merge(value);

    /// <inheritdoc cref="InertiaResponseFactory.Merge(object?)"/>
    public static MergeProp Merge(Func<object?> callback) => InertiaResponseFactory.Merge(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Merge(object?)"/>
    public static MergeProp Merge(Func<Task<object?>> callback) => InertiaResponseFactory.Merge(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Merge(object?)"/>
    public static MergeProp Merge(Func<IServiceProvider, object?> callback) => InertiaResponseFactory.Merge(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Merge(object?)"/>
    public static MergeProp Merge(Func<IServiceProvider, Task<object?>> callback) => InertiaResponseFactory.Merge(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Merge(object?)"/>
    public static MergeProp<T> Merge<T>(T? value) => InertiaResponseFactory.Merge(value);

    /// <inheritdoc cref="InertiaResponseFactory.Merge(object?)"/>
    public static MergeProp<T> Merge<T>(Func<T?> callback) => InertiaResponseFactory.Merge(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Merge(object?)"/>
    public static MergeProp<T> Merge<T>(Func<Task<T?>> callback) => InertiaResponseFactory.Merge(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Merge(object?)"/>
    public static MergeProp<T> Merge<T>(Func<IServiceProvider, T?> callback) => InertiaResponseFactory.Merge(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Merge(object?)"/>
    public static MergeProp<T> Merge<T>(Func<IServiceProvider, Task<T?>> callback) => InertiaResponseFactory.Merge(callback);

    // Once

    /// <inheritdoc cref="InertiaResponseFactory.Once(Func{object})"/>
    public static OnceProp Once(Func<object?> callback) => InertiaResponseFactory.Once(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Once(Func{object})"/>
    public static OnceProp Once(Func<Task<object?>> callback) => InertiaResponseFactory.Once(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Once(Func{object})"/>
    public static OnceProp Once(Func<IServiceProvider, object?> callback) => InertiaResponseFactory.Once(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Once(Func{object})"/>
    public static OnceProp Once(Func<IServiceProvider, Task<object?>> callback) => InertiaResponseFactory.Once(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Once(Func{object})"/>
    public static OnceProp<T> Once<T>(Func<T?> callback) => InertiaResponseFactory.Once(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Once(Func{object})"/>
    public static OnceProp<T> Once<T>(Func<Task<T?>> callback) => InertiaResponseFactory.Once(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Once(Func{object})"/>
    public static OnceProp<T> Once<T>(Func<IServiceProvider, T?> callback) => InertiaResponseFactory.Once(callback);

    /// <inheritdoc cref="InertiaResponseFactory.Once(Func{object})"/>
    public static OnceProp<T> Once<T>(Func<IServiceProvider, Task<T?>> callback) => InertiaResponseFactory.Once(callback);

    // Scroll

    /// <inheritdoc cref="InertiaResponseFactory.Scroll{T}(T, string, IProvidesScrollMetadata)"/>
    public static ScrollProp<T> Scroll<T>(T? value, string wrapper = "data", IProvidesScrollMetadata? metadataProvider = null) =>
        InertiaResponseFactory.Scroll(value, wrapper, metadataProvider);

    /// <inheritdoc cref="InertiaResponseFactory.Scroll{T}(T, string, IProvidesScrollMetadata)"/>
    public static ScrollProp<T> Scroll<T>(Func<T?> callback, string wrapper = "data", IProvidesScrollMetadata? metadataProvider = null) =>
        InertiaResponseFactory.Scroll(callback, wrapper, metadataProvider);

    // -- Private helpers --

    private static IInertiaResponseFactory GetFactory()
    {
        var context = GetHttpContext();
        return context.RequestServices.GetRequiredService<IInertiaResponseFactory>();
    }

    private static HttpContext GetHttpContext()
    {
        if (s_httpContextAccessor?.HttpContext == null)
        {
            throw new InvalidOperationException(
                "Inertia static helper is not initialized. Call services.AddInertiaStaticHelper() " +
                "in your service configuration, or inject IInertiaResponseFactory directly.");
        }

        return s_httpContextAccessor.HttpContext;
    }
}
