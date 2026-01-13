using System;

namespace Swap.Htmx.State;

/// <summary>
/// Marks a SwapState property as protected, ensuring it is encrypted in hidden fields
/// and URLs, and verified upon binding.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class SwapProtectedAttribute : Attribute
{
}

/// <summary>
/// Marks a SwapState property as unprotected (plain text), even if the container
/// is configured to be protected by default.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class SwapUnprotectedAttribute : Attribute
{
}
