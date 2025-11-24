namespace Swap.Htmx.Realtime.Redis;

public class RedisSseOptions
{
    public string Configuration { get; set; } = "localhost";
    public string InstanceName { get; set; } = "Swap";
    public string ChannelName { get; set; } = "swap-sse-events";
}
