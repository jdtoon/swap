using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Swap.Htmx.State;

/// <summary>
/// Base class for HTMX state containers that automatically track changes
/// and serialize to hidden form fields.
/// </summary>
/// <remarks>
/// Inherit from this class to define your application state:
/// <code>
/// public class InventoryState : SwapState
/// {
///     public string Tab { get; set; } = "all";
///     public int Page { get; set; } = 1;
///     public string? Search { get; set; }
/// }
/// </code>
/// 
/// Then use with [FromSwapState] for automatic model binding:
/// <code>
/// public IActionResult Grid([FromSwapState] InventoryState state)
/// {
///     // state is automatically populated from hidden fields
/// }
/// </code>
/// </remarks>
public abstract class SwapState : INotifyPropertyChanged
{
    private readonly HashSet<string> _changedProperties = new();
    private bool _trackChanges = true;

    /// <summary>
    /// Event raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the unique identifier for this state container.
    /// Override to customize the container ID.
    /// </summary>
    /// <remarks>
    /// Defaults to the class name in kebab-case with "-state" suffix.
    /// For example, "InventoryState" becomes "inventory-state".
    /// </remarks>
    public virtual string ContainerId => ToKebabCase(GetType().Name);

    /// <summary>
    /// Gets the names of properties that have been modified since creation or last reset.
    /// </summary>
    public IReadOnlySet<string> ChangedProperties => _changedProperties;

    /// <summary>
    /// Returns true if any property has been modified.
    /// </summary>
    public bool HasChanges => _changedProperties.Count > 0;

    /// <summary>
    /// Clears the change tracking state.
    /// </summary>
    public void AcceptChanges()
    {
        _changedProperties.Clear();
    }

    /// <summary>
    /// Temporarily disables change tracking for bulk updates.
    /// </summary>
    public IDisposable SuspendChangeTracking()
    {
        return new ChangeTrackingSuspension(this);
    }

    /// <summary>
    /// Gets all state properties and their current values.
    /// </summary>
    /// <returns>Dictionary of property names to values.</returns>
    public IDictionary<string, object?> GetStateValues()
    {
        var result = new Dictionary<string, object?>();
        var properties = GetStateProperties();

        foreach (var prop in properties)
        {
            result[prop.Name] = prop.GetValue(this);
        }

        return result;
    }

    /// <summary>
    /// Sets state values from a dictionary (used by model binder).
    /// </summary>
    /// <param name="values">Dictionary of property names to values.</param>
    public void SetStateValues(IDictionary<string, object?> values)
    {
        using (SuspendChangeTracking())
        {
            var properties = GetStateProperties().ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in values)
            {
                if (properties.TryGetValue(kvp.Key, out var prop) && prop.CanWrite)
                {
                    try
                    {
                        var convertedValue = ConvertValue(kvp.Value, prop.PropertyType);
                        prop.SetValue(this, convertedValue);
                    }
                    catch
                    {
                        // Skip properties that can't be converted
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the properties that should be serialized as state.
    /// Override to customize which properties are included.
    /// </summary>
    protected virtual IEnumerable<PropertyInfo> GetStateProperties()
    {
        return GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !IsExcludedProperty(p.Name))
            .Where(p => IsSupportedType(p.PropertyType));
    }

    /// <summary>
    /// Called by property setters to notify of changes.
    /// </summary>
    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;

        if (_trackChanges && propertyName != null)
        {
            _changedProperties.Add(propertyName);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (_trackChanges && propertyName != null)
        {
            _changedProperties.Add(propertyName);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static bool IsExcludedProperty(string name)
    {
        return name is nameof(ContainerId) or nameof(ChangedProperties) or nameof(HasChanges);
    }

    private static bool IsSupportedType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlying.IsPrimitive
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(Guid)
            || underlying.IsEnum;
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                ? Activator.CreateInstance(targetType)
                : null;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is string strValue)
        {
            if (string.IsNullOrEmpty(strValue) && Nullable.GetUnderlyingType(targetType) != null)
                return null;

            if (underlying == typeof(string))
                return strValue;

            if (underlying == typeof(int))
                return int.TryParse(strValue, out var i) ? i : 0;

            if (underlying == typeof(long))
                return long.TryParse(strValue, out var l) ? l : 0L;

            if (underlying == typeof(decimal))
                return decimal.TryParse(strValue, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m;

            if (underlying == typeof(double))
                return double.TryParse(strValue, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var dbl) ? dbl : 0d;

            if (underlying == typeof(bool))
                return strValue.Equals("true", StringComparison.OrdinalIgnoreCase) 
                    || strValue == "1" 
                    || strValue.Equals("on", StringComparison.OrdinalIgnoreCase);

            if (underlying == typeof(DateTime))
                return DateTime.TryParse(strValue, out var dt) ? dt : DateTime.MinValue;

            if (underlying == typeof(Guid))
                return Guid.TryParse(strValue, out var g) ? g : Guid.Empty;

            if (underlying.IsEnum)
                return Enum.TryParse(underlying, strValue, ignoreCase: true, out var e) ? e : Activator.CreateInstance(underlying);
        }

        return Convert.ChangeType(value, underlying);
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new System.Text.StringBuilder();
        
        for (int i = 0; i < value.Length; i++)
        {
            var c = value[i];
            
            if (char.IsUpper(c))
            {
                if (i > 0)
                    result.Append('-');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        // Remove "-state" suffix if present, then add it back to ensure consistency
        var str = result.ToString();
        if (!str.EndsWith("-state"))
            str += "-state";

        return str;
    }

    private sealed class ChangeTrackingSuspension : IDisposable
    {
        private readonly SwapState _state;
        private readonly bool _previousValue;

        public ChangeTrackingSuspension(SwapState state)
        {
            _state = state;
            _previousValue = state._trackChanges;
            state._trackChanges = false;
        }

        public void Dispose()
        {
            _state._trackChanges = _previousValue;
        }
    }
}
