using System.Text.Json;
using Swap.Htmx.ServerSentEvents;

namespace SwapChat.Services;

/// <summary>
/// A simple file-based backplane to demonstrate distributed SSE.
/// This allows multiple instances of the app to communicate via a shared file.
/// </summary>
public class FileSseBackplane : ISseBackplane, IDisposable
{
    private readonly string _filePath;
    private readonly List<Func<SseMessage, CancellationToken, Task>> _handlers = new();
    private readonly FileSystemWatcher _watcher;
    private readonly ILogger<FileSseBackplane> _logger;
    private DateTime _lastRead = DateTime.MinValue;

    public FileSseBackplane(IWebHostEnvironment env, ILogger<FileSseBackplane> logger)
    {
        _logger = logger;
        var directory = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "bus.jsonl");

        // Ensure file exists
        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "");
        }

        _watcher = new FileSystemWatcher(directory, "bus.jsonl")
        {
            NotifyFilter = NotifyFilters.LastWrite
        };

        _watcher.Changed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    public async Task PublishAsync(SseMessage message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        // Simple append. In production, use a real message bus (Redis, NATS, RabbitMQ).
        // We use a retry policy here because file locking might occur with multiple processes.
        await WriteWithRetryAsync(json + Environment.NewLine);
    }

    public Task SubscribeAsync(Func<SseMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
    {
        _handlers.Add(handler);
        return Task.CompletedTask;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce slightly or just fire and forget
        _ = ReadNewMessagesAsync();
    }

    private async Task ReadNewMessagesAsync()
    {
        try
        {
            // Give the writer a moment to release the lock
            await Task.Delay(50);

            var lines = await ReadAllLinesWithRetryAsync();
            var newLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            
            // In a real file bus, we'd track offsets. 
            // Here we'll just read the last few lines and check timestamps or IDs if we had them.
            // For this demo, we'll just grab the last line if it's new.
            // Actually, let's just read the whole file and keep an in-memory pointer (line count).
            // But since we don't persist state, let's just read the last line for simplicity 
            // and assume low traffic for the demo.
            
            if (newLines.Count > 0)
            {
                var lastLine = newLines.Last();
                try 
                {
                    var message = JsonSerializer.Deserialize<SseMessage>(lastLine);
                    if (message != null)
                    {
                        // Dispatch to all local subscribers
                        foreach (var handler in _handlers)
                        {
                            await handler(message, CancellationToken.None);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse message from file bus.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file bus.");
        }
    }

    private async Task WriteWithRetryAsync(string content)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await File.AppendAllTextAsync(_filePath, content);
                return;
            }
            catch (IOException)
            {
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error writing to file bus: {Path}", _filePath);
                throw; // Re-throw non-IO exceptions
            }
        }
        _logger.LogWarning("Failed to write to file bus after 5 attempts: {Path}", _filePath);
    }

    private async Task<string[]> ReadAllLinesWithRetryAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                return await File.ReadAllLinesAsync(_filePath);
            }
            catch (IOException)
            {
                await Task.Delay(100);
            }
        }
        return Array.Empty<string>();
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}
