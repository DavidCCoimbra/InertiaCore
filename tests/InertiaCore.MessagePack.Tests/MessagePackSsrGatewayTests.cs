using InertiaCore.Configuration;
using InertiaCore.MessagePack;
using InertiaCore.Ssr;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.MessagePack.Tests;

[Trait("Class", "MessagePackSsrGateway")]
public class MessagePackSsrGatewayTests
{
    // -- Gateway behavior --

    [Fact]
    public async Task RenderAsync_returns_null_when_disabled()
    {
        var gateway = CreateGateway(o => o.Enabled = false);

        var result = await gateway.RenderAsync(CreatePage());

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_returns_null_on_socket_error()
    {
        var gateway = CreateGateway(o =>
        {
            o.Enabled = true;
            o.SocketPath = "/tmp/nonexistent-inertia-test.sock";
        });

        var result = await gateway.RenderAsync(CreatePage());

        Assert.Null(result);
    }

    [Fact]
    public async Task IsHealthyAsync_returns_false_when_disabled()
    {
        var gateway = CreateGateway(o => o.Enabled = false);

        var result = await gateway.IsHealthyAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsHealthyAsync_returns_false_when_socket_unreachable()
    {
        var gateway = CreateGateway(o =>
        {
            o.Enabled = true;
            o.SocketPath = "/tmp/nonexistent-inertia-test.sock";
        });

        var result = await gateway.IsHealthyAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task RenderAsync_handles_cancellation()
    {
        var gateway = CreateGateway(o =>
        {
            o.Enabled = true;
            o.SocketPath = "/tmp/nonexistent-inertia-test.sock";
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await gateway.RenderAsync(CreatePage(), cts.Token);

        Assert.Null(result);
    }

    // -- MessagePack serialization verification --

    [Fact]
    public void MessagePack_serializes_page_object()
    {
        var page = CreatePage();
        var options = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolverAllowPrivate.Instance);

        var bytes = MessagePackSerializer.Serialize(page, options);

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void MessagePack_is_smaller_than_json()
    {
        var page = CreateLargePage();
        var msgpackOptions = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolverAllowPrivate.Instance)
            .WithCompression(MessagePackCompression.Lz4BlockArray);

        var msgpackBytes = MessagePackSerializer.Serialize(page, msgpackOptions);
        var jsonBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(page);

        Assert.True(msgpackBytes.Length < jsonBytes.Length,
            $"MessagePack ({msgpackBytes.Length} bytes) should be smaller than JSON ({jsonBytes.Length} bytes)");
    }

    [Fact]
    public void MessagePack_roundtrip_preserves_data()
    {
        var page = CreatePage();
        var options = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolverAllowPrivate.Instance);

        var bytes = MessagePackSerializer.Serialize(page, options);
        var deserialized = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(bytes, options);

        Assert.Equal("Test/Component", deserialized["component"]);
        Assert.Equal("/test", deserialized["url"]);
    }

    [Fact]
    public void MessagePack_with_lz4_compression_works()
    {
        var page = CreateLargePage();
        var options = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolverAllowPrivate.Instance)
            .WithCompression(MessagePackCompression.Lz4BlockArray);

        var compressed = MessagePackSerializer.Serialize(page, options);
        var deserialized = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(compressed, options);

        Assert.Equal("Users/Index", deserialized["component"]);
    }

    // -- Helpers --

    private static MessagePackSsrGateway CreateGateway(Action<SsrOptions>? configure = null)
    {
        var options = new InertiaOptions();
        configure?.Invoke(options.Ssr);

        return new MessagePackSsrGateway(
            Options.Create(options),
            Substitute.For<ILogger<MessagePackSsrGateway>>());
    }

    private static Dictionary<string, object?> CreatePage() => new()
    {
        ["component"] = "Test/Component",
        ["props"] = new Dictionary<string, object?> { ["name"] = "Alice" },
        ["url"] = "/test",
        ["version"] = "1.0.0",
    };

    private static Dictionary<string, object?> CreateLargePage() => new()
    {
        ["component"] = "Users/Index",
        ["url"] = "/users",
        ["version"] = "1.0.0",
        ["props"] = new Dictionary<string, object?>
        {
            ["users"] = Enumerable.Range(1, 100).Select(i => new Dictionary<string, object?>
            {
                ["id"] = i,
                ["name"] = $"User {i}",
                ["email"] = $"user{i}@example.com",
                ["role"] = i % 3 == 0 ? "admin" : "user",
            }).ToList(),
            ["pagination"] = new Dictionary<string, object?>
            {
                ["currentPage"] = 1,
                ["totalPages"] = 10,
                ["perPage"] = 100,
            },
        },
    };
}
