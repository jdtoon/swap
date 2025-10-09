using NetMX.Ddd.Application.Services;

namespace NetMX.Ddd.Application.Services;

public abstract class ApplicationService : IApplicationService
{
    // We will add common properties like IUnitOfWorkManager, ILogger, etc. here later.
    // For now, it serves as the concrete implementation of the IApplicationService marker.
}