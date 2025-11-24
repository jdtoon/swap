using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Swap.Htmx.Realtime;

namespace Swap.Htmx.Realtime.Redis;

public class RedisSseBackplane : ISseBackplane
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisSseOptions _options;
    private readonly ILogger<RedisSseBackplane> _logger;
    private readonly ISubscriber _subscriber;
    private readonly RedisChannel _channel;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisSseBackplane(
        IConnectionMultiplexer redis,
        IOptions<RedisSseOptions> options,
        ILogger<RedisSseBackplane> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
        _subscriber = _redis.GetSubscriber();
        _channel = RedisChannel.Literal(_options.ChannelName);
        _jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task PublishAsync(SseMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            await _subscriber.PublishAsync(_channel, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish SSE message to Redis.");
            throw;
        }
    }

    public async Task SubscribeAsync(Func<SseMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
    {
        await _subscriber.SubscribeAsync(_channel, async (channel, value) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<SseMessage>(value.ToString(), _jsonOptions);
                if (message != null)
                {
                    // We pass CancellationToken.None here because the Redis callback doesn't provide one
                    // and the original token passed to SubscribeAsync was for the subscription operation itself,
                    // not for the lifetime of the subscription.
                    await handler(message, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SSE message from Redis.");
            }
        });
    }
}
