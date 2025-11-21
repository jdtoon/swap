using System;

namespace Swap.Htmx.Attributes;

/// <summary>
/// Marks a partial class as a source for Swap Event generation.
/// The source generator will scan this class for string constants and generate
/// a corresponding hierarchy of strongly-typed EventKey properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SwapEventSourceAttribute : Attribute
{
}