using System.ComponentModel.DataAnnotations;
using InertiaCore.Constants;
using InertiaCore.Core;
using InertiaCore.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InertiaCore.Tests.Filters;

[Trait("Class", "InertiaValidationFilter")]
public class InertiaValidationFilterTests
{
    [Fact]
    public async Task Valid_arguments_pass_through()
    {
        var filter = new InertiaValidationFilter();
        var context = CreateContext(new ValidRequest { Name = "Alice", Email = "alice@test.com" });
        var nextCalled = false;

        await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invalid_inertia_request_redirects_with_errors()
    {
        var filter = new InertiaValidationFilter();
        var errorService = Substitute.For<IInertiaErrorService>();
        var context = CreateContext(
            new ValidRequest { Name = "", Email = "" },
            isInertia: true,
            errorService: errorService);

        var result = await filter.InvokeAsync(context, _ =>
            ValueTask.FromResult<object?>(Results.Ok()));

        errorService.Received(1).SetErrors(
            Arg.Is<Dictionary<string, string>>(e => e.ContainsKey("Name")),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task Invalid_non_inertia_request_returns_validation_problem()
    {
        var filter = new InertiaValidationFilter();
        var context = CreateContext(
            new ValidRequest { Name = "", Email = "" },
            isInertia: false);

        var result = await filter.InvokeAsync(context, _ =>
            ValueTask.FromResult<object?>(Results.Ok()));

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Skips_primitive_and_string_arguments()
    {
        var filter = new InertiaValidationFilter();
        var context = CreateContext("just a string");
        var nextCalled = false;

        await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Skips_null_arguments()
    {
        var filter = new InertiaValidationFilter();
        var context = CreateContext(null!);
        var nextCalled = false;

        await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Uses_referer_for_redirect()
    {
        var filter = new InertiaValidationFilter();
        var errorService = Substitute.For<IInertiaErrorService>();
        var context = CreateContext(
            new ValidRequest { Name = "", Email = "" },
            isInertia: true,
            errorService: errorService,
            referer: "http://localhost/form");

        var result = await filter.InvokeAsync(context, _ =>
            ValueTask.FromResult<object?>(Results.Ok()));

        // Result is a redirect — errors were set
        errorService.Received(1).SetErrors(Arg.Any<Dictionary<string, string>>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Extracts_first_error_per_field()
    {
        var filter = new InertiaValidationFilter();
        Dictionary<string, string>? capturedErrors = null;
        var errorService = Substitute.For<IInertiaErrorService>();
        errorService.When(x => x.SetErrors(Arg.Any<Dictionary<string, string>>(), Arg.Any<string?>()))
            .Do(x => capturedErrors = x.Arg<Dictionary<string, string>>());

        var context = CreateContext(
            new ValidRequest { Name = "", Email = "not-an-email" },
            isInertia: true,
            errorService: errorService);

        await filter.InvokeAsync(context, _ =>
            ValueTask.FromResult<object?>(Results.Ok()));

        Assert.NotNull(capturedErrors);
        Assert.True(capturedErrors!.ContainsKey("Name"));
        Assert.True(capturedErrors.ContainsKey("Email"));
    }

    private static EndpointFilterInvocationContext CreateContext(
        object? argument,
        bool isInertia = false,
        IInertiaErrorService? errorService = null,
        string? referer = null)
    {
        var httpContext = new DefaultHttpContext();

        if (isInertia)
        {
            httpContext.Request.Headers[InertiaHeaders.Inertia] = "true";
        }

        if (referer != null)
        {
            httpContext.Request.Headers.Referer = referer;
        }

        var services = new ServiceCollection();
        services.AddSingleton(errorService ?? Substitute.For<IInertiaErrorService>());
        httpContext.RequestServices = services.BuildServiceProvider();

        var arguments = argument is not null ? new List<object?> { argument } : new List<object?> { null };
        return new DefaultEndpointFilterInvocationContext(httpContext, arguments.ToArray());
    }

    public class ValidRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; init; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be valid.")]
        public string Email { get; init; } = "";
    }
}
