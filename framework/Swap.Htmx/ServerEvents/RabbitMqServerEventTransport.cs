using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.ServerEvents;

public sealed class RabbitMqServerEventTransport : IServerEventTransport, IDisposable
{
    public sealed class Options
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = AmqpTcpEndpoint.UseDefaultPort;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string ExchangeName { get; set; } = "swap.events";
    public string ExchangeKind { get; set; } = RabbitMQ.Client.ExchangeType.Topic;
        public bool DurableExchange { get; set; } = true;
        public bool AutoDeleteExchange { get; set; } = false;
        public ushort PrefetchCount { get; set; } = 50;
        public string? ClientProvidedName { get; set; }
    }

    private readonly Options _opts;
    private readonly object _connGate = new();
    private IConnection? _conn;

    public RabbitMqServerEventTransport(Options opts)
    {
        _opts = opts ?? throw new ArgumentNullException(nameof(opts));
    }

    private IConnection EnsureConnection()
    {
        if (_conn is { IsOpen: true }) return _conn;
        lock (_connGate)
        {
            if (_conn is { IsOpen: true }) return _conn;
            var factory = new ConnectionFactory
            {
                HostName = _opts.HostName,
                Port = _opts.Port,
                UserName = _opts.UserName,
                Password = _opts.Password,
                VirtualHost = _opts.VirtualHost,
                ClientProvidedName = _opts.ClientProvidedName ?? "Swap.Htmx.ServerEvents"
            };
            // Basic retry to avoid crashing when the broker is not yet ready (e.g., on container start)
            const int maxAttempts = 10;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _conn = factory.CreateConnection();
                    using var ch = _conn.CreateModel();
                    ch.ExchangeDeclare(exchange: _opts.ExchangeName, type: _opts.ExchangeKind, durable: _opts.DurableExchange, autoDelete: _opts.AutoDeleteExchange);
                    return _conn;
                }
                catch
                {
                    if (attempt == maxAttempts) throw;
                    try { System.Threading.Thread.Sleep(TimeSpan.FromSeconds(Math.Min(5, attempt))); } catch { }
                }
            }
            return _conn!;
        }
    }

    public Task PublishAsync(string eventKey, ReadOnlyMemory<byte> payload, IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        var conn = EnsureConnection();
        using var ch = conn.CreateModel();
        var props = ch.CreateBasicProperties();
        props.DeliveryMode = 2; // persistent
        if (headers is { Count: > 0 })
        {
            props.Headers ??= new Dictionary<string, object?>();
            foreach (var kvp in headers) props.Headers[kvp.Key] = kvp.Value;
        }
        ch.BasicPublish(exchange: _opts.ExchangeName, routingKey: eventKey, basicProperties: props, body: payload);
        return Task.CompletedTask;
    }

    public IDisposable Subscribe(string eventKey, Func<ReadOnlyMemory<byte>, IReadOnlyDictionary<string, string>, CancellationToken, Task> onMessage)
    {
        var conn = EnsureConnection();
        var ch = conn.CreateModel();
        ch.BasicQos(0, _opts.PrefetchCount, global: false);
        // Per-eventKey shared queue pattern; durable, non-exclusive, non-auto-delete by default
        var queue = ch.QueueDeclare(queue: $"swap.events.{Sanitize(eventKey)}", durable: true, exclusive: false, autoDelete: false);
        ch.QueueBind(queue: queue.QueueName, exchange: _opts.ExchangeName, routingKey: eventKey);

        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var dict = new Dictionary<string, string>(StringComparer.Ordinal);
                if (ea.BasicProperties?.Headers is { Count: > 0 })
                {
                    foreach (var kvp in ea.BasicProperties.Headers)
                    {
                        try
                        {
                            if (kvp.Value is byte[] b) dict[kvp.Key] = Encoding.UTF8.GetString(b);
                            else if (kvp.Value is ReadOnlyMemory<byte> rm) dict[kvp.Key] = Encoding.UTF8.GetString(rm.Span);
                            else if (kvp.Value is string s) dict[kvp.Key] = s;
                            else if (kvp.Value is object o) dict[kvp.Key] = o.ToString() ?? string.Empty;
                        }
                        catch { }
                    }
                }
                await onMessage(ea.Body.ToArray(), dict, CancellationToken.None).ConfigureAwait(false);
                ch.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch
            {
                ch.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        var consumerTag = ch.BasicConsume(queue: queue.QueueName, autoAck: false, consumer: consumer);

        return new Subscription(ch, _opts.ExchangeName, queue.QueueName, eventKey, consumerTag);
    }

    private static string Sanitize(string key) => key.Replace('*', '_').Replace('#', '_').Replace(' ', '.');

    private sealed class Subscription : IDisposable
    {
        private readonly IModel _channel;
        private readonly string _exchange;
        private readonly string _queue;
        private readonly string _routingKey;
        private readonly string _consumerTag;
        private int _disposed;

        public Subscription(IModel channel, string exchange, string queue, string routingKey, string consumerTag)
        {
            _channel = channel; _exchange = exchange; _queue = queue; _routingKey = routingKey; _consumerTag = consumerTag;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            try { _channel.BasicCancel(_consumerTag); } catch { }
            try { _channel.QueueUnbind(_queue, _exchange, _routingKey); } catch { }
            try { _channel.Close(); _channel.Dispose(); } catch { }
        }
    }

    public void Dispose()
    {
        try { _conn?.Close(); _conn?.Dispose(); } catch { }
        _conn = null;
    }
}

public static class RabbitMqServerEventTransportServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqServerEventTransport(this IServiceCollection services, Action<RabbitMqServerEventTransport.Options> configure)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var opts = new RabbitMqServerEventTransport.Options();
        configure(opts);
        return services.AddSingleton<IServerEventTransport>(sp => new RabbitMqServerEventTransport(opts));
    }
}
