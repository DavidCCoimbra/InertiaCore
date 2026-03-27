namespace InertiaCore.Core;

/// <summary>
/// Accumulates metadata during prop resolution and builds the metadata dictionary for the page object.
/// </summary>
internal sealed class PropMetadataCollector
{
    private readonly List<string> _sharedPropKeys = [];
    private readonly List<string> _pageDataProps = [];
    private readonly Dictionary<string, List<string>> _deferredProps = [];
    private readonly List<string> _mergeProps = [];
    private readonly List<string> _deepMergeProps = [];
    private readonly List<string> _prependProps = [];
    private readonly List<string> _matchPropsOn = [];
    private readonly Dictionary<string, object?> _onceProps = [];

    public void TrackSharedKeys(IEnumerable<string> keys) =>
        _sharedPropKeys.AddRange(keys);

    public void AddPageData(string key) => _pageDataProps.Add(key);

    public void AddDeferred(string group, string path)
    {
        if (!_deferredProps.TryGetValue(group, out var groupList))
        {
            groupList = [];
            _deferredProps[group] = groupList;
        }

        groupList.Add(path);
    }

    public void AddMerge(string path) => _mergeProps.Add(path);

    public void AddDeepMerge(string path) => _deepMergeProps.Add(path);

    public void AddPrepend(string path) => _prependProps.Add(path);

    public void AddMatchOn(string[] keys) => _matchPropsOn.AddRange(keys);

    public void AddOnce(string path, long? expiresAt)
    {
        _onceProps[path] = new Dictionary<string, object?>
        {
            ["prop"] = path,
            ["expiresAt"] = expiresAt,
        };
    }

    public Dictionary<string, object?> Build()
    {
        var metadata = new Dictionary<string, object?>();

        if (_deferredProps.Count > 0)
        {
            metadata["deferredProps"] = _deferredProps;
        }

        if (_mergeProps.Count > 0)
        {
            metadata["mergeProps"] = _mergeProps;
        }

        if (_deepMergeProps.Count > 0)
        {
            metadata["deepMergeProps"] = _deepMergeProps;
        }

        if (_prependProps.Count > 0)
        {
            metadata["prependProps"] = _prependProps;
        }

        if (_matchPropsOn.Count > 0)
        {
            metadata["matchPropsOn"] = _matchPropsOn;
        }

        if (_onceProps.Count > 0)
        {
            metadata["onceProps"] = _onceProps;
        }

        if (_sharedPropKeys.Count > 0)
        {
            metadata["sharedProps"] = _sharedPropKeys;
        }

        if (_pageDataProps.Count > 0)
        {
            metadata["pageDataProps"] = _pageDataProps;
        }

        return metadata;
    }
}
