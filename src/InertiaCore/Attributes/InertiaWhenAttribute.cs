namespace InertiaCore.Attributes;

/// <summary>
/// Conditionally includes a prop based on a boolean property on the same record.
/// When the condition property is false, the prop is excluded entirely (not null — absent).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InertiaWhenAttribute : Attribute
{
    /// <summary>
    /// Name of the boolean property on the same type to evaluate.
    /// </summary>
    public string ConditionProperty { get; }

    /// <summary>
    /// Creates a conditional prop attribute.
    /// </summary>
    public InertiaWhenAttribute(string conditionProperty)
    {
        ConditionProperty = conditionProperty;
    }
}
