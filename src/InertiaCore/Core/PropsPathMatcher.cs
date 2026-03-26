using Microsoft.AspNetCore.Http;

namespace InertiaCore.Core;

/// <summary>
/// Handles bidirectional prefix matching for partial reload filtering.
/// </summary>
internal sealed class PropsPathMatcher
{
    private readonly HashSet<string> _only;
    private readonly HashSet<string> _except;

    public PropsPathMatcher(HashSet<string> only, HashSet<string> except)
    {
        _only = only;
        _except = except;
    }

    public bool MatchesOnly(string path)
    {
        return _only.Any(only =>
            path == only || path.StartsWith($"{only}.", StringComparison.Ordinal));
    }

    public bool LeadsToOnly(string path)
    {
        return _only.Any(only =>
            only.StartsWith($"{path}.", StringComparison.Ordinal));
    }

    public bool MatchesExcept(string path)
    {
        return _except.Any(except =>
            path == except || path.StartsWith($"{except}.", StringComparison.Ordinal));
    }

    public bool HasOnlyFilter => _only.Count > 0;

    public bool HasExceptFilter => _except.Count > 0;

    public static HashSet<string> ParseHeader(HttpRequest? request, string headerName)
    {
        var value = request?.Headers[headerName].FirstOrDefault();
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        return [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }
}
