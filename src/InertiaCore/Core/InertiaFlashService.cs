using System.Text.Json;
using InertiaCore.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Core;

/// <summary>
/// Scoped service for managing flash data that persists through one redirect via TempData.
/// </summary>
public class InertiaFlashService : IInertiaFlashService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<string, object?> _pendingFlash = new();

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaFlashService"/>.
    /// </summary>
    public InertiaFlashService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Stores a flash value for the next response.
    /// </summary>
    public void Flash(string key, object? value) =>
        _pendingFlash[key] = value;

    /// <summary>
    /// Stores multiple flash values for the next response.
    /// </summary>
    public void Flash(Dictionary<string, object?> data)
    {
        foreach (var (key, value) in data)
        {
            _pendingFlash[key] = value;
        }
    }

    /// <summary>
    /// Returns the pending flash data for this request.
    /// </summary>
    public Dictionary<string, object?> GetPending() => _pendingFlash;

    /// <summary>
    /// Persists pending flash data to TempData so it survives the redirect.
    /// Called by the middleware after the endpoint executes.
    /// </summary>
    public void Persist()
    {
        if (_pendingFlash.Count == 0)
        {
            return;
        }

        var tempData = GetTempData();
        if (tempData == null)
        {
            return;
        }

        tempData[SessionKeys.FlashData] = JsonSerializer.Serialize(_pendingFlash);
        tempData.Save();
    }

    /// <summary>
    /// Consumes flash data from TempData and returns it. Returns null if no flash data.
    /// Called by InertiaResponse to merge flash into shared props.
    /// </summary>
    public Dictionary<string, object?>? Consume()
    {
        var tempData = GetTempData();
        if (tempData == null)
        {
            return null;
        }

        if (!tempData.TryGetValue(SessionKeys.FlashData, out var raw) || raw is not string flashJson)
        {
            return null;
        }

        var flash = JsonSerializer.Deserialize<Dictionary<string, object?>>(flashJson);
        tempData.Save();

        return flash is { Count: > 0 } ? flash : null;
    }

    /// <summary>
    /// Keeps flash data alive through a redirect by preventing TempData consumption.
    /// Called by the middleware on redirect responses.
    /// </summary>
    public void Reflash()
    {
        var tempData = GetTempData();
        if (tempData == null)
        {
            return;
        }

        if (!tempData.ContainsKey(SessionKeys.FlashData))
        {
            return;
        }

        tempData.Keep(SessionKeys.FlashData);
        tempData.Save();
    }

    private ITempDataDictionary? GetTempData()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        var tempDataFactory = httpContext.RequestServices.GetService<ITempDataDictionaryFactory>();
        return tempDataFactory?.GetTempData(httpContext);
    }
}
