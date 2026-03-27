using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using InertiaCore.Attributes;
using InertiaCore.Contracts;
using InertiaCore.Props;

namespace InertiaCore.Core;

/// <summary>
/// Resolves Inertia prop attributes on typed props and wraps values in the appropriate prop types.
/// Validates attribute combinations and caches metadata per type for performance.
/// </summary>
internal static class PropAttributeResolver
{
    private static readonly ConcurrentDictionary<Type, PropPropertyInfo[]> s_cache = new();

    /// <summary>
    /// Converts an object's properties to a dictionary, wrapping values based on Inertia attributes.
    /// If a value is already an IInertiaProp, attributes are skipped (explicit wins).
    /// </summary>
    public static Dictionary<string, object?> ConvertToPropsDict(object props, List<string>? pageDataKeys = null)
    {
        var type = props.GetType();
        var properties = s_cache.GetOrAdd(type, ResolveProperties);
        var dict = new Dictionary<string, object?>(properties.Length);

        foreach (var prop in properties)
        {
            // [InertiaWhen] — skip prop entirely if condition is false
            if (prop.When is not null)
            {
                var conditionProp = type.GetProperty(prop.When.ConditionProperty);
                if (conditionProp?.GetValue(props) is not true)
                {
                    continue;
                }
            }

            var value = prop.Property.GetValue(props);
            var key = JsonNamingPolicy.CamelCase.ConvertName(prop.Property.Name);
            dict[key] = value is IInertiaProp
                ? value
                : prop.HasAttributes ? WrapValue(prop, value) : value;

            if (prop.PageData is not null)
            {
                pageDataKeys?.Add(key);
            }
        }

        return dict;
    }

    private static PropPropertyInfo[] ResolveProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var result = new PropPropertyInfo[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var info = new PropPropertyInfo(
                property,
                property.GetCustomAttribute<InertiaAlwaysAttribute>(),
                property.GetCustomAttribute<InertiaDeferAttribute>(),
                property.GetCustomAttribute<InertiaMergeAttribute>(),
                property.GetCustomAttribute<InertiaOnceAttribute>(),
                property.GetCustomAttribute<InertiaOptionalAttribute>(),
                property.GetCustomAttribute<InertiaLiveAttribute>(),
                property.GetCustomAttribute<InertiaWhenAttribute>(),
                property.GetCustomAttribute<InertiaFallbackAttribute>(),
                property.GetCustomAttribute<InertiaTimedAttribute>(),
                property.GetCustomAttribute<InertiaPageDataAttribute>());

            ValidateAttributes(info, type);
            result[i] = info;
        }

        return result;
    }

    private static void ValidateAttributes(PropPropertyInfo info, Type type)
    {
        if (!info.HasAttributes)
        {
            return;
        }

        var name = info.Property.Name;
        var typeName = type.Name;

        // Always cannot combine with anything
        if (info.Always is not null &&
            (info.Defer is not null || info.Merge is not null || info.Once is not null || info.Optional is not null))
        {
            throw new InvalidOperationException(
                $"Property '{name}' on type '{typeName}' has invalid attribute combination: " +
                "[InertiaAlways] cannot be combined with other Inertia attributes. " +
                "It indicates the prop is always included — no additional behavior applies.");
        }

        // Multiple base attributes (Defer + Optional)
        if (info.Defer is not null && info.Optional is not null)
        {
            throw new InvalidOperationException(
                $"Property '{name}' on type '{typeName}' has conflicting Inertia attributes: " +
                "[InertiaDefer] and [InertiaOptional] are both base prop types. " +
                "Use only one base attribute per property. " +
                "To combine behaviors, use a base + modifiers: [InertiaDefer] + [InertiaMerge] + [InertiaOnce].");
        }

        // Merge + Optional is invalid (OptionalProp doesn't implement IMergeable)
        if (info.Merge is not null && info.Optional is not null)
        {
            throw new InvalidOperationException(
                $"Property '{name}' on type '{typeName}' has invalid attribute combination: " +
                "[InertiaMerge] cannot be used with [InertiaOptional] because OptionalProp does not support merge behavior. " +
                "[InertiaMerge] can only be combined with [InertiaDefer].");
        }

        // Deep + Prepend on same merge attribute is contradictory
        if (info.Merge is { Deep: true, Prepend: true })
        {
            throw new InvalidOperationException(
                $"Property '{name}' on type '{typeName}' has invalid attribute combination: " +
                "[InertiaMerge] cannot have both Deep and Prepend enabled. " +
                "Deep merge recursively merges objects; Prepend controls array insertion order. Choose one.");
        }
    }

    private static object? WrapValue(PropPropertyInfo info, object? value)
    {
        // Base: Defer
        if (info.Defer is not null)
        {
            var prop = new DeferProp(() => value, info.Defer.Group);
            ApplyMerge(prop, info.Merge);
            ApplyOnce(prop, info.Once);
            ApplyLive(prop, info.Live);
            ApplyFallback(prop, info.Fallback);
            ApplyTimed(prop, info.Timed);
            return prop;
        }

        // Base: Optional
        if (info.Optional is not null)
        {
            var prop = new OptionalProp(() => value);
            ApplyOnce(prop, info.Once);
            ApplyLive(prop, info.Live);
            ApplyFallback(prop, info.Fallback);
            ApplyTimed(prop, info.Timed);
            return prop;
        }

        // Base: Merge
        if (info.Merge is not null)
        {
            var prop = new MergeProp(value);
            ApplyMerge(prop, info.Merge);
            ApplyOnce(prop, info.Once);
            ApplyLive(prop, info.Live);
            ApplyFallback(prop, info.Fallback);
            ApplyTimed(prop, info.Timed);
            return prop;
        }

        // Base: Once
        if (info.Once is not null)
        {
            var prop = new OnceProp(() => value);
            ApplyOnce(prop, info.Once);
            ApplyLive(prop, info.Live);
            ApplyFallback(prop, info.Fallback);
            ApplyTimed(prop, info.Timed);
            return prop;
        }

        // Base: Always
        if (info.Always is not null)
        {
            var prop = new AlwaysProp(value);
            ApplyLive(prop, info.Live);
            ApplyFallback(prop, info.Fallback);
            ApplyTimed(prop, info.Timed);
            return prop;
        }

        return value;
    }

    private static void ApplyMerge(IMergeable prop, InertiaMergeAttribute? merge)
    {
        if (merge is null)
        {
            return;
        }

        if (merge.Deep)
        {
            prop.Merge.EnableDeepMerge();
        }
        else if (merge.Prepend)
        {
            prop.Merge.Prepend();
        }
        else
        {
            prop.Merge.EnableMerge();
        }
    }

    private static void ApplyOnce(IOnceable prop, InertiaOnceAttribute? once)
    {
        if (once is null)
        {
            return;
        }

        prop.Once.EnableOnce();

        if (once.Key is not null)
        {
            prop.Once.SetKey(once.Key);
        }

        if (once.TtlSeconds > 0)
        {
            prop.Once.SetTtl(TimeSpan.FromSeconds(once.TtlSeconds));
        }
    }

    private static void ApplyLive(ILiveProp prop, InertiaLiveAttribute? live)
    {
        if (live is null)
        {
            return;
        }

        prop.Live.Enable(live.Channel);
    }

    private static void ApplyFallback(IFallbackProp prop, InertiaFallbackAttribute? fallback)
    {
        if (fallback is null)
        {
            return;
        }

        var value = Activator.CreateInstance(fallback.FallbackType);
        prop.Fallback.SetFallback(value);
    }

    private static void ApplyTimed(ITimedProp prop, InertiaTimedAttribute? timed)
    {
        if (timed is null || timed.IntervalSeconds <= 0)
        {
            return;
        }

        prop.Timed.SetInterval(TimeSpan.FromSeconds(timed.IntervalSeconds));
    }

    private sealed record PropPropertyInfo(
        PropertyInfo Property,
        InertiaAlwaysAttribute? Always,
        InertiaDeferAttribute? Defer,
        InertiaMergeAttribute? Merge,
        InertiaOnceAttribute? Once,
        InertiaOptionalAttribute? Optional,
        InertiaLiveAttribute? Live,
        InertiaWhenAttribute? When,
        InertiaFallbackAttribute? Fallback,
        InertiaTimedAttribute? Timed,
        InertiaPageDataAttribute? PageData = null)
    {
        public bool HasAttributes =>
            Always is not null || Defer is not null || Merge is not null ||
            Once is not null || Optional is not null || Live is not null ||
            When is not null || Fallback is not null || Timed is not null;
    }
}
