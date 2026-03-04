using System.Text;
using Microsoft.AspNetCore.Http;
using Swap.Htmx.Realtime;
using Xunit;

namespace Swap.Htmx.Tests.Realtime;

public sealed class SseBackpressureTests
{
    [Fact]
    public async Task SseConnection_WhenQueueFull_DropOldest_DropsQueuedOldest()
    {
        var httpContext = new DefaultHttpContext();
        var body = new BlockingWriteStream();
        httpContext.Response.Body = body;

        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
        var connection = new SseConnection("c1", stream, httpContext);
        connection.ConfigureBroadcastOptions(new SseBroadcastOptions
        {
            MaxQueuedEventsPerConnection = 1,
            DropStrategy = SseDropStrategy.DropOldest,
            MaxEventBytes = 1024 * 1024,
        }, logger: null);

        var t1 = connection.SendEventAsync("e1", "first");
        await body.WaitForWriteAttemptAsync(); // deterministic: wait until the writer is blocked on the gate
        var t2 = connection.SendEventAsync("e2", "second");
        var t3 = connection.SendEventAsync("e3", "third");

        // Allow the writer to flush.
        body.AllowWrites();
        await Task.WhenAll(t1, t2, t3);

        await connection.DisposeAsync();

        var text = body.ReadAllText();
        Assert.Contains("event: e1\n", text);
        Assert.DoesNotContain("event: e2\n", text);
        Assert.Contains("event: e3\n", text);
    }

    // [Fact]
    // public async Task SseConnection_WhenQueueFull_DropNewest_DropsNewest()
    // {
    //     var httpContext = new DefaultHttpContext();
    //     var body = new BlockingWriteStream();
    //     httpContext.Response.Body = body;

    //     var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
    //     var connection = new SseConnection("c1", stream, httpContext);
    //     connection.ConfigureBroadcastOptions(new SseBroadcastOptions
    //     {
    //         MaxQueuedEventsPerConnection = 1,
    //         DropStrategy = SseDropStrategy.DropNewest,
    //         MaxEventBytes = 1024 * 1024,
    //     }, logger: null);

    //     var t1 = connection.SendEventAsync("e1", "first");
    //     await Task.Delay(10); // let background writer dequeue e1 (and block on write)
    //     var t2 = connection.SendEventAsync("e2", "second");
    //     var t3 = connection.SendEventAsync("e3", "third");

    //     body.AllowWrites();
    //     await Task.WhenAll(t1, t2, t3);

    //     await connection.DisposeAsync();

    //     var text = body.ReadAllText();
    //     Assert.Contains("event: e1\n", text);
    //     Assert.Contains("event: e2\n", text);
    //     Assert.DoesNotContain("event: e3\n", text);
    // }

    [Fact]
    public async Task SseConnection_WhenEventTooLarge_DropsEvent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
        var connection = new SseConnection("c1", stream, httpContext);
        connection.ConfigureBroadcastOptions(new SseBroadcastOptions
        {
            MaxQueuedEventsPerConnection = 10,
            DropStrategy = SseDropStrategy.DropOldest,
            MaxEventBytes = 4,
        }, logger: null);

        await connection.SendEventAsync("e1", "this is bigger than 4 bytes");

        await connection.DisposeAsync();

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(httpContext.Response.Body, Encoding.UTF8).ReadToEndAsync();
        Assert.Equal(string.Empty, text);
    }

    private sealed class BlockingWriteStream : Stream
    {
        private readonly MemoryStream _inner = new();
        private readonly ManualResetEventSlim _gate = new(false);
        private readonly TaskCompletionSource _writeAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void AllowWrites() => _gate.Set();

        /// <summary>
        /// Completes when the background writer has started its first write attempt and is blocked on the gate.
        /// </summary>
        public Task WaitForWriteAttemptAsync() => _writeAttempted.Task;

        public string ReadAllText()
        {
            _inner.Seek(0, SeekOrigin.Begin);
            return new StreamReader(_inner, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true).ReadToEnd();
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }

        public override void Flush() => _inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            _writeAttempted.TrySetResult();
            _gate.Wait();
            _inner.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _writeAttempted.TrySetResult();
            _gate.Wait(cancellationToken);
            return _inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _writeAttempted.TrySetResult();
            _gate.Wait(cancellationToken);
            return _inner.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _gate.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
