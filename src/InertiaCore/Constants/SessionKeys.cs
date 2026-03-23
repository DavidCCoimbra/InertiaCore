namespace InertiaCore.Constants;

/// <summary>
/// Session and TempData key constants used by the Inertia protocol.
/// </summary>
public static class SessionKeys
{
    /// <summary>
    /// Session key for clearing the Inertia history.
    /// </summary>
    public const string ClearHistory = "inertia.clear_history";

    /// <summary>
    /// Session key for flash data.
    /// </summary>
    public const string FlashData = "inertia.flash_data";

    /// <summary>
    /// Session key for preserving the URL fragment.
    /// </summary>
    public const string PreserveFragment = "inertia.preserve_fragment";
}

