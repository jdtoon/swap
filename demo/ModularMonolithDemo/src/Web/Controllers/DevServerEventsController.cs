using Microsoft.AspNetCore.Mvc;
using Swap.Htmx.ServerEvents;
using Swap.Modularity.Abstractions;

namespace ModularMonolithDemo.Web.Controllers;

[ApiController]
[Route("dev/server-events")] 
public class DevServerEventsController : ControllerBase
{
    private readonly IEventChainRegistrar _registrar;
    private readonly IServiceProvider _services;
    public DevServerEventsController(IEventChainRegistrar registrar, IServiceProvider services)
    {
        _registrar = registrar; _services = services;
    }

    [HttpGet("info")]
    public IActionResult Info()
    {
        var transport = _services.GetService(typeof(IServerEventTransport)) as IServerEventTransport;
        return Ok(new
        {
            Registrar = _registrar.GetType().FullName,
            Transport = transport?.GetType().FullName ?? "(none)",
        });
    }
}
