using System;
using System.Collections.Generic;

namespace Swap.Htmx.Flows;

/// <summary>
/// A server-authoritative, pure step-machine for multi-step flows and wizards. Subclasses declare
/// the ordered <see cref="Steps"/>, and this base class tracks the current position and enforces
/// per-step <see cref="SwapFlowStep.CanEnter"/> guards during navigation.
/// </summary>
/// <remarks>
/// This type has no dependency on HTTP or <c>SwapState</c>; callers are responsible for persisting
/// and restoring <see cref="CurrentIndex"/> (e.g. via <see cref="RestoreIndex"/>) across requests.
/// </remarks>
public abstract class SwapFlow
{
    /// <summary>
    /// The ordered list of steps that make up this flow. Must be non-empty.
    /// </summary>
    protected abstract IReadOnlyList<SwapFlowStep> Steps { get; }

    /// <summary>
    /// The zero-based index of the current step.
    /// </summary>
    public int CurrentIndex { get; private set; }

    /// <summary>
    /// The step at <see cref="CurrentIndex"/>.
    /// </summary>
    public SwapFlowStep Current => GetSteps()[CurrentIndex];

    /// <summary>
    /// <see langword="true"/> when there is a step after the current one.
    /// </summary>
    public bool CanGoNext => CurrentIndex < GetSteps().Count - 1;

    /// <summary>
    /// <see langword="true"/> when there is a step before the current one.
    /// </summary>
    public bool CanGoPrevious => CurrentIndex > 0;

    /// <summary>
    /// Advances to the next step if its <see cref="SwapFlowStep.CanEnter"/> guard (if any) allows it.
    /// If the immediate next step's guard fails, the current position is unchanged.
    /// </summary>
    /// <returns><see langword="true"/> if the position moved forward; otherwise <see langword="false"/>.</returns>
    public bool Next()
    {
        var steps = GetSteps();
        if (CurrentIndex >= steps.Count - 1)
        {
            return false;
        }

        var nextStep = steps[CurrentIndex + 1];
        if (nextStep.CanEnter is not null && !nextStep.CanEnter())
        {
            return false;
        }

        CurrentIndex++;
        return true;
    }

    /// <summary>
    /// Moves back one step, without evaluating any guard.
    /// </summary>
    /// <returns><see langword="true"/> if the position moved backward; otherwise <see langword="false"/>.</returns>
    public bool Previous()
    {
        if (CurrentIndex <= 0)
        {
            return false;
        }

        CurrentIndex--;
        return true;
    }

    /// <summary>
    /// Jumps to the step at <paramref name="index"/>, clamped into the valid range. If the target
    /// step has a <see cref="SwapFlowStep.CanEnter"/> guard that returns <see langword="false"/>,
    /// the position is unchanged.
    /// </summary>
    /// <param name="index">The desired step index; out-of-range values are clamped.</param>
    /// <returns><see langword="true"/> if the position moved; otherwise <see langword="false"/>.</returns>
    public bool GoTo(int index)
    {
        var steps = GetSteps();
        var target = Clamp(index, steps.Count);

        var targetStep = steps[target];
        if (targetStep.CanEnter is not null && !targetStep.CanEnter())
        {
            return false;
        }

        CurrentIndex = target;
        return true;
    }

    /// <summary>
    /// Restores a persisted position (e.g. round-tripped from <c>SwapState</c>), clamped into the
    /// valid range. Unlike <see cref="GoTo"/>, this ignores <see cref="SwapFlowStep.CanEnter"/>
    /// guards, since the position was already validated when it was originally reached.
    /// </summary>
    /// <param name="index">The step index to restore; out-of-range values are clamped.</param>
    public void RestoreIndex(int index)
    {
        var steps = GetSteps();
        CurrentIndex = Clamp(index, steps.Count);
    }

    private IReadOnlyList<SwapFlowStep> GetSteps()
    {
        var steps = Steps;
        if (steps is null || steps.Count == 0)
        {
            throw new InvalidOperationException("SwapFlow.Steps must contain at least one step.");
        }

        return steps;
    }

    private static int Clamp(int index, int count)
    {
        if (index < 0)
        {
            return 0;
        }

        if (index > count - 1)
        {
            return count - 1;
        }

        return index;
    }
}
