using System.Collections.Generic;
using Swap.Htmx.Flows;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapFlowTests
{
    private sealed class TestFlow : SwapFlow
    {
        public bool AllowStep2 { get; set; }

        protected override IReadOnlyList<SwapFlowStep> Steps { get; }

        public TestFlow(bool allowStep2 = true)
        {
            AllowStep2 = allowStep2;
            Steps = new List<SwapFlowStep>
            {
                new("step1", "Views/Step1.cshtml"),
                new("step2", "Views/Step2.cshtml", () => AllowStep2),
                new("step3", "Views/Step3.cshtml"),
            };
        }
    }

    private sealed class EmptyFlow : SwapFlow
    {
        protected override IReadOnlyList<SwapFlowStep> Steps { get; } = new List<SwapFlowStep>();
    }

    [Fact]
    public void Current_ReflectsCurrentIndex()
    {
        var flow = new TestFlow();

        Assert.Equal(0, flow.CurrentIndex);
        Assert.Equal("step1", flow.Current.Name);
    }

    [Fact]
    public void Next_Advances_AndClampsAtEnd()
    {
        var flow = new TestFlow();

        Assert.True(flow.Next());
        Assert.Equal("step2", flow.Current.Name);
        Assert.True(flow.CanGoNext);

        Assert.True(flow.Next());
        Assert.Equal("step3", flow.Current.Name);
        Assert.False(flow.CanGoNext);

        Assert.False(flow.Next());
        Assert.Equal("step3", flow.Current.Name);
    }

    [Fact]
    public void Previous_GoesBack_AndClampsAtZero()
    {
        var flow = new TestFlow();
        flow.Next();
        flow.Next();

        Assert.True(flow.Previous());
        Assert.Equal("step2", flow.Current.Name);
        Assert.True(flow.CanGoPrevious);

        Assert.True(flow.Previous());
        Assert.Equal("step1", flow.Current.Name);
        Assert.False(flow.CanGoPrevious);

        Assert.False(flow.Previous());
        Assert.Equal("step1", flow.Current.Name);
    }

    [Fact]
    public void Next_BlockedByGuard_StaysPut()
    {
        var flow = new TestFlow(allowStep2: false);

        Assert.False(flow.Next());
        Assert.Equal(0, flow.CurrentIndex);
        Assert.Equal("step1", flow.Current.Name);
    }

    [Fact]
    public void GoTo_ClampsOutOfRangeIndex()
    {
        var flow = new TestFlow();

        Assert.True(flow.GoTo(100));
        Assert.Equal("step3", flow.Current.Name);

        Assert.True(flow.GoTo(-100));
        Assert.Equal("step1", flow.Current.Name);
    }

    [Fact]
    public void GoTo_RespectsCanEnterGuard()
    {
        var flow = new TestFlow(allowStep2: false);

        Assert.False(flow.GoTo(1));
        Assert.Equal(0, flow.CurrentIndex);
        Assert.Equal("step1", flow.Current.Name);
    }

    [Fact]
    public void RestoreIndex_ClampsIntoRange_IgnoringGuards()
    {
        var flow = new TestFlow(allowStep2: false);

        flow.RestoreIndex(1);
        Assert.Equal(1, flow.CurrentIndex);
        Assert.Equal("step2", flow.Current.Name);

        flow.RestoreIndex(100);
        Assert.Equal(2, flow.CurrentIndex);

        flow.RestoreIndex(-100);
        Assert.Equal(0, flow.CurrentIndex);
    }

    [Fact]
    public void EmptySteps_ThrowsInvalidOperationException()
    {
        var flow = new EmptyFlow();

        Assert.Throws<InvalidOperationException>(() => flow.Current);
    }
}
