using System.Text.Json;
using InertiaCore.Constants;
using InertiaCore.Props;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace InertiaCore.Core;

/// <summary>
/// Scoped service for managing validation errors that persist through one redirect via TempData.
/// </summary>
public sealed class InertiaErrorService : IInertiaErrorService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaErrorService"/>.
    /// </summary>
    public InertiaErrorService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public void ShareErrors(IInertiaResponseFactory factory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        factory.Share("errors", new AlwaysProp(_ => (object?)ConsumeErrors()));
    }

    /// <inheritdoc />
    public void SetErrors(Dictionary<string, string> errors, string? errorBag = null)
    {
        if (errors.Count == 0)
        {
            return;
        }

        var tempData = GetTempData();
        if (tempData == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(errorBag))
        {
            tempData[SessionKeys.Errors] = JsonSerializer.Serialize(
                new Dictionary<string, object> { [errorBag] = errors });
        }
        else
        {
            tempData[SessionKeys.Errors] = JsonSerializer.Serialize(errors);
        }

        tempData.Save();
    }

    /// <inheritdoc />
    public void Reflash()
    {
        var tempData = GetTempData();
        if (tempData == null)
        {
            return;
        }

        if (!tempData.ContainsKey(SessionKeys.Errors))
        {
            return;
        }

        tempData.Keep(SessionKeys.Errors);
        tempData.Save();
    }

    private Dictionary<string, object?> ConsumeErrors()
    {
        var tempData = GetTempData();
        if (tempData == null)
        {
            return new();
        }

        if (!tempData.TryGetValue(SessionKeys.Errors, out var raw) || raw is not string json)
        {
            return new();
        }

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
    }

    private ITempDataDictionary? GetTempData() =>
        TempDataAccessor.GetTempData(_httpContextAccessor);
}
