using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;

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
    /// Gets whether tampering was detected while reading a protected value for this state.
    /// A protected field (via <see cref="Protected"/> or <c>[SwapProtected]</c>) that is present
    /// in the request but empty, or that fails to decrypt/verify, sets this to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// When binding via <c>[FromSwapState]</c>, a tampered state fails model binding and records a
    /// <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary"/> error, so a tampered
    /// request never reaches your action as a valid model. This flag is also set by
    /// <see cref="FromQueryString"/> so callers using it directly can inspect the result.
    /// </remarks>
    public bool Tampered { get; internal set; }

    /// <summary>
    /// Gets or sets whether this state should be synchronized with the URL query string.
    /// When enabled, state properties are appended to URLs and read from query parameters.
    /// </summary>
    /// <remarks>
    /// Enable URL sync for state that should be bookmarkable or shareable.
    /// This works in conjunction with hx-push-url="true" to maintain browser history.
    /// </remarks>
    public virtual bool UrlSync => false;

    /// <summary>
    /// Gets the prefix used for URL parameters when UrlSync is enabled.
    /// Override to customize the prefix. Defaults to empty (no prefix).
    /// </summary>
    /// <remarks>
    /// Useful when multiple state containers are on the same page to avoid parameter conflicts.
    /// For example, with prefix "inv", the "Page" property becomes "invPage" in the URL.
    /// </remarks>
    public virtual string UrlPrefix => string.Empty;

    /// <summary>
    /// Gets the list of property names that should be excluded from URL sync.
    /// Override to exclude sensitive or large properties from the URL.
    /// </summary>
    protected virtual IEnumerable<string> UrlExcludedProperties => Array.Empty<string>();

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
    /// Generates a query string from the current state values for URL synchronization.
    /// Only includes non-default values to keep URLs clean.
    /// </summary>
    /// <param name="protectionProvider">Optional protection provider for encrypting values.</param>
    /// <returns>A query string like "?Page=2&amp;Tab=active" or empty if at defaults.</returns>
    public string ToQueryString(IDataProtectionProvider? protectionProvider = null)
    {
        if (!UrlSync)
            return string.Empty;

        var excludedProps = new HashSet<string>(UrlExcludedProperties, StringComparer.OrdinalIgnoreCase);
        var parts = new List<string>();
        var defaultInstance = CreateDefaultInstance();
        var properties = GetStateProperties();

        foreach (var prop in properties)
        {
            if (excludedProps.Contains(prop.Name))
                continue;

            var currentValue = prop.GetValue(this);
            var defaultValue = defaultInstance != null ? prop.GetValue(defaultInstance) : GetDefaultForType(prop.PropertyType);

            // Only include non-default values
            if (!ValuesEqual(currentValue, defaultValue))
            {
                var paramName = string.IsNullOrEmpty(UrlPrefix) ? prop.Name : $"{UrlPrefix}{prop.Name}";
                var valueStr = FormatUrlValue(currentValue);

                if (valueStr != null)
                {
                    if (protectionProvider != null && SwapStateRenderer.IsPropertyProtected(this, prop.Name))
                    {
                        var protector = protectionProvider.CreateProtector("SwapState", ContainerId, prop.Name);
                        valueStr = protector.Protect(valueStr);
                    }

                    parts.Add($"{Uri.EscapeDataString(paramName)}={Uri.EscapeDataString(valueStr)}");
                }
            }
        }

        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

    /// <summary>
    /// Populates state properties from an HTTP query string.
    /// </summary>
    /// <param name="query">The query string collection from the request.</param>
    /// <param name="protectionProvider">Optional protection provider for decrypting values.</param>
    public void FromQueryString(IQueryCollection query, IDataProtectionProvider? protectionProvider = null)
    {
        if (!UrlSync || query == null)
            return;

        using (SuspendChangeTracking())
        {
            var excludedProps = new HashSet<string>(UrlExcludedProperties, StringComparer.OrdinalIgnoreCase);
            var properties = GetStateProperties().ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var key in query.Keys)
            {
                // Handle prefixed parameters
                var propName = key;
                if (!string.IsNullOrEmpty(UrlPrefix) && key.StartsWith(UrlPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    propName = key.Substring(UrlPrefix.Length);
                }
                else if (!string.IsNullOrEmpty(UrlPrefix))
                {
                    continue; // Skip parameters that don't match our prefix
                }

                if (excludedProps.Contains(propName))
                    continue;

                if (properties.TryGetValue(propName, out var prop) && prop.CanWrite)
                {
                    var rawValues = query[key];
                    // Use the first value (URL priority or input priority)
                    var stringValue = rawValues.Count > 0 ? rawValues[0] : null;

                    if (stringValue == null) continue;
                    
                    if (protectionProvider != null && SwapStateRenderer.IsPropertyProtected(this, prop.Name))
                    {
                        try
                        {
                            var protector = protectionProvider.CreateProtector("SwapState", ContainerId, prop.Name);
                            stringValue = protector.Unprotect(stringValue);
                        }
                        catch
                        {
                            // Protected value present but empty or failed to decrypt/verify.
                            // Fail closed: flag tampering and never apply the forged value.
                            Tampered = true;
                            continue;
                        }
                    }

                    try
                    {
                        var convertedValue = ConvertValue(stringValue, prop.PropertyType);
                        prop.SetValue(this, convertedValue);
                    }
                    catch
                    {
                        // Skip parameters that can't be converted
                    }
                }
            }
        }
    }

    /// <summary>
    /// Appends current state to a base URL as query parameters.
    /// </summary>
    /// <param name="baseUrl">The base URL to append state to.</param>
    /// <param name="protectionProvider">Optional protection provider.</param>
    /// <returns>The URL with state parameters appended.</returns>
    public string AppendToUrl(string baseUrl, IDataProtectionProvider? protectionProvider = null)
    {
        if (!UrlSync)
            return baseUrl;

        var queryString = ToQueryString(protectionProvider);
        if (string.IsNullOrEmpty(queryString))
            return baseUrl;

        var separator = baseUrl.Contains("?") ? "&" : "";
        var query = queryString.TrimStart('?');
        
        return baseUrl.Contains("?") 
            ? $"{baseUrl}&{query}" 
            : $"{baseUrl}{queryString}";
    }

    private SwapState? CreateDefaultInstance()
    {
        try
        {
            return (SwapState?)Activator.CreateInstance(GetType());
        }
        catch
        {
            return null;
        }
    }

    private static object? GetDefaultForType(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return a.Equals(b);
    }

    private static string? FormatUrlValue(object? value)
    {
        if (value == null)
            return null;

        return value switch
        {
            bool b => b ? "true" : "false",
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
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
    /// Per-Type cache of reflected state property metadata, so the property set for a given
    /// <see cref="SwapState"/> subclass is computed once via reflection and reused for every
    /// instance, instead of re-running <see cref="Type.GetProperties()"/> and the LINQ filters
    /// on every render/bind.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, StateMetadata> _metadataCache = new();

    /// <summary>
    /// Gets the properties that should be serialized as state.
    /// Override to customize which properties are included.
    /// </summary>
    /// <remarks>
    /// The default implementation returns a cached, per-Type array of filtered
    /// <see cref="PropertyInfo"/> values, computed once via reflection per concrete
    /// <see cref="SwapState"/> subclass and reused across all instances of that type.
    /// </remarks>
    protected virtual IEnumerable<PropertyInfo> GetStateProperties()
    {
        return GetCachedMetadata(GetType()).Properties;
    }

    /// <summary>
    /// Gets (computing and caching if necessary) the reflection metadata for the given
    /// <see cref="SwapState"/>-derived <paramref name="type"/>.
    /// </summary>
    private static StateMetadata GetCachedMetadata(Type type)
    {
        return _metadataCache.GetOrAdd(type, static t => new StateMetadata(t));
    }

    /// <summary>
    /// Computes the filtered set of state properties for a given <see cref="SwapState"/> subclass.
    /// </summary>
    private static PropertyInfo[] ComputeStateProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !IsExcludedProperty(p.Name))
            .Where(p => IsSupportedType(p.PropertyType))
            .ToArray();
    }

    /// <summary>
    /// Immutable, per-Type reflection metadata cached for a <see cref="SwapState"/> subclass.
    /// Contains only Type-level (attribute/reflection-derived) data; per-instance state such as
    /// <see cref="Protected"/> is never cached here.
    /// </summary>
    private sealed class StateMetadata
    {
        public StateMetadata(Type type)
        {
            Properties = ComputeStateProperties(type);
            PropertyNames = Properties.Select(p => p.Name).ToFrozenSet(StringComparer.Ordinal);
        }

        /// <summary>
        /// The filtered, public instance, readable+writable, non-excluded, supported-type
        /// properties for the state type.
        /// </summary>
        public PropertyInfo[] Properties { get; }

        /// <summary>
        /// The names of <see cref="Properties"/>, as a frozen set for fast lookup.
        /// </summary>
        public FrozenSet<string> PropertyNames { get; }
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
        return name is nameof(ContainerId) or nameof(ChangedProperties) or nameof(HasChanges) or nameof(Tampered);
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

    /// <summary>
    /// Gets whether this state should be encrypted/signed to prevent tampering.
    /// When enabled, values in hidden fields are protected using IDataProtection.
    /// Default is false. Override to true to enable protection.
    /// </summary>
    public virtual bool Protected => false;

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
