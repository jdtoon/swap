using NetMX.Ddd.Application.Services;

namespace NetMX.Ddd.Application.Services;

/// <summary>
/// Base class for application services providing common functionality.
/// </summary>
public abstract class ApplicationService : IApplicationService
{
    // We will add common properties like IUnitOfWorkManager, ILogger, etc. here later.
    // For now, it serves as the concrete implementation of the IApplicationService marker.
}