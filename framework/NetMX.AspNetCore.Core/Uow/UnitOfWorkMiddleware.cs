using Microsoft.AspNetCore.Http;
using NetMX.Ddd.Application.Uow;
using System.Threading.Tasks;

namespace NetMX.AspNetCore.Uow;

public class UnitOfWorkMiddleware : IMiddleware
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public UnitOfWorkMiddleware(IUnitOfWorkManager unitOfWorkManager)
    {
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // We will add logic here to check the UnitOfWorkAttribute
        // to see if the UoW should be transactional or disabled.
        // For now, we start a default one for every request.

        using (var uow = _unitOfWorkManager.Begin())
        {
            await next(context);
            await uow.CompleteAsync();
        }
    }
}