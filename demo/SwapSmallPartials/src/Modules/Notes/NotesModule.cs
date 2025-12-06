using SwapSmallPartials.Modules.Notes.Services;

namespace SwapSmallPartials.Modules.Notes;

public static class NotesModule
{
    public static IServiceCollection AddNotesModule(this IServiceCollection services)
    {
        services.AddScoped<INotesService, NotesService>();
        return services;
    }
}
