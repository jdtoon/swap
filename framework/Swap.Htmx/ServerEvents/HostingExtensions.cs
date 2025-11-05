using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.ServerEvents;

public static class ServerEventHostingExtensions
{
    /// <summary>
    /// Registers the server event chain registrar based on configuration.
    /// Defaults to in-memory; if ServerEvents:Transport=RabbitMq, registers RabbitMQ transport and distributed registrar.
    /// </summary>
    public static IServiceCollection AddSwapServerEventChainsFromConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var transport = configuration["ServerEvents:Transport"] ?? "InMemory";
        if (string.Equals(transport, "RabbitMq", StringComparison.OrdinalIgnoreCase))
        {
            services.AddRabbitMqServerEventTransport(opts =>
            {
                opts.HostName = configuration["ServerEvents:RabbitMq:HostName"] ?? "localhost";
                if (int.TryParse(configuration["ServerEvents:RabbitMq:Port"], out var port)) opts.Port = port;
                opts.UserName = configuration["ServerEvents:RabbitMq:UserName"] ?? "guest";
                opts.Password = configuration["ServerEvents:RabbitMq:Password"] ?? "guest";
                opts.VirtualHost = configuration["ServerEvents:RabbitMq:VirtualHost"] ?? "/";
                opts.ExchangeName = configuration["ServerEvents:RabbitMq:ExchangeName"] ?? "swap.events";
                opts.ClientProvidedName = configuration["ServerEvents:RabbitMq:ClientName"] ?? "Swap.App";
            });
            services.AddSwapServerEventChainsDistributed();
        }
        else
        {
            services.AddSwapServerEventChains();
        }
        return services;
    }
}
