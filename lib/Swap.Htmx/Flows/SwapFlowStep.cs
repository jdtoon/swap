namespace Swap.Htmx.Flows;

/// <summary>
/// Describes a single step in a <see cref="SwapFlow"/>.
/// </summary>
/// <param name="Name">The unique, stable identifier for the step.</param>
/// <param name="ViewName">The view (or partial view) name to render for this step.</param>
/// <param name="CanEnter">
/// An optional guard evaluated before entering the step. When it returns <see langword="false"/>,
/// navigation to this step is blocked.
/// </param>
public sealed record SwapFlowStep(string Name, string ViewName, System.Func<bool>? CanEnter = null);
