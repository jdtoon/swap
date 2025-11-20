using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using TaskFlow.Services;
using TaskFlow.Views;
using TaskFlow.Models;

namespace TaskFlow.Events;

public class ProjectEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        // Project Created
        config.When(ProjectEvents.Created)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ctx =>
            {
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .Toast("Project created", ToastType.Success);

        // Project Progress Changed
        config.When(ProjectEvents.ProgressChanged)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                // Payload would contain project data if passed
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .Toast("Project progress updated", ToastType.Info);
    }
}
