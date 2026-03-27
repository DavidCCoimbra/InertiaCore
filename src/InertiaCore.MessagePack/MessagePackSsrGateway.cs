using System.Net.Sockets;
using System.Text.Json;
using InertiaCore.Configuration;
using InertiaCore.Ssr;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InertiaCore.MessagePack;

/// <summary>
/// SSR gateway that uses MessagePack binary serialization over Unix Domain Sockets
/// for minimal serialization and transport overhead (~4x faster IPC than JSON over HTTP).
/// </summary>
public sealed partial class MessagePackSsrGateway : ISsrGateway
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly MessagePackSerializerOptions s_msgpackOptions =
        MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolverAllowPrivate.Instance)
            .WithCompression(MessagePackCompression.Lz4BlockArray);

    private readonly SsrOptions _ssrOptions;
    private readonly ILogger<MessagePackSsrGateway> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="MessagePackSsrGateway"/>.
    /// </summary>
    public MessagePackSsrGateway(
        IOptions<InertiaOptions> options,
        ILogger<MessagePackSsrGateway> logger)
    {
        _ssrOptions = options.Value.Ssr;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SsrResponse?> RenderAsync(
        Dictionary<string, object?> page,
        CancellationToken cancellationToken = default)
    {
        if (!_ssrOptions.Enabled)
        {
            return null;
        }

        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.SendTimeout = _ssrOptions.TimeoutSeconds * 1000;
            socket.ReceiveTimeout = _ssrOptions.TimeoutSeconds * 1000;

            await socket.ConnectAsync(
                new UnixDomainSocketEndPoint(_ssrOptions.SocketPath),
                cancellationToken);

            // Serialize page object to MessagePack binary
            var payload = MessagePackSerializer.Serialize(page, s_msgpackOptions, cancellationToken);

            LogSsrRequest(_logger, page.TryGetValue("component", out var comp) ? comp?.ToString() : "unknown", payload.Length);

            // Write length-prefixed message
            var lengthPrefix = BitConverter.GetBytes(payload.Length);
            await socket.SendAsync(lengthPrefix, SocketFlags.None, cancellationToken);
            await socket.SendAsync(payload, SocketFlags.None, cancellationToken);

            // Signal end of send
            socket.Shutdown(SocketShutdown.Send);

            // Read response (JSON — strings don't benefit from binary encoding)
            var responseBytes = await ReadFullResponseAsync(socket, cancellationToken);
            var data = JsonSerializer.Deserialize<SsrResponseData>(responseBytes, s_jsonOptions);

            if (data is null)
            {
                LogSsrWarning(_logger, "SSR returned null response", null);
                return null;
            }

            return new SsrResponse(
                Head: string.Join("\n", data.Head ?? []),
                Body: data.Body ?? "");
        }
        catch (SocketException ex)
        {
            LogSsrWarning(_logger, $"SSR sidecar not reachable at {_ssrOptions.SocketPath}", ex);
            return null;
        }
        catch (MessagePackSerializationException ex)
        {
            LogSsrWarning(_logger, "MessagePack serialization failed", ex);
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            LogSsrWarning(_logger, "SSR MessagePack gateway error", ex);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (!_ssrOptions.Enabled)
        {
            return false;
        }

        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await socket.ConnectAsync(
                new UnixDomainSocketEndPoint(_ssrOptions.SocketPath),
                cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<byte[]> ReadFullResponseAsync(Socket socket, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[4096];

        while (true)
        {
            var read = await socket.ReceiveAsync(buffer, SocketFlags.None, ct);
            if (read == 0)
            {
                break;
            }

            ms.Write(buffer, 0, read);
        }

        return ms.ToArray();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "SSR msgpack request: {Component}, payload {Size} bytes")]
    private static partial void LogSsrRequest(ILogger logger, string? component, int size);

    [LoggerMessage(Level = LogLevel.Warning, Message = "SSR msgpack: {Message}")]
    private static partial void LogSsrWarning(ILogger logger, string message, Exception? exception);

    private sealed record SsrResponseData(string[]? Head, string? Body);
}
