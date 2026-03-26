using System.ComponentModel.DataAnnotations;
using InertiaCore.Core;
using InertiaCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Filters;

/// <summary>
/// Endpoint filter for minimal APIs that validates bound arguments using DataAnnotations.
/// If validation fails on an Inertia request, redirects back with errors automatically.
/// </summary>
public sealed class InertiaValidationFilter : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var errors = ValidateArguments(context);

        if (errors.Count == 0)
        {
            return await next(context);
        }

        if (!context.HttpContext.IsInertiaRequest())
        {
            var problemErrors = errors.ToDictionary(e => e.Key, e => new[] { e.Value });
            return Results.ValidationProblem(problemErrors);
        }

        var errorService = context.HttpContext.RequestServices.GetRequiredService<IInertiaErrorService>();
        errorService.SetErrors(errors);

        var referer = context.HttpContext.Request.Headers.Referer.FirstOrDefault() ?? "/";
        return Results.Redirect(referer);
    }

    private static Dictionary<string, string> ValidateArguments(EndpointFilterInvocationContext context)
    {
        var errors = new Dictionary<string, string>();

        foreach (var argument in context.Arguments)
        {
            if (argument is null)
            {
                continue;
            }

            var type = argument.GetType();
            if (type.IsPrimitive || type == typeof(string) || type == typeof(CancellationToken))
            {
                continue;
            }

            var results = new List<ValidationResult>();
            var validationContext = new ValidationContext(argument);

            if (Validator.TryValidateObject(argument, validationContext, results, validateAllProperties: true))
            {
                continue;
            }

            foreach (var result in results)
            {
                var fieldName = result.MemberNames.FirstOrDefault() ?? "general";
                errors.TryAdd(fieldName, result.ErrorMessage ?? "Validation failed.");
            }
        }

        return errors;
    }
}
