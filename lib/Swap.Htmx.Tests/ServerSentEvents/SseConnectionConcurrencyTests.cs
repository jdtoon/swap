using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Swap.Htmx.Realtime;
using Xunit;

namespace Swap.Htmx.Tests.ServerSentEvents;

/// <summary>
/// Concurrency correctness for the SSE hot path: writes must be serialized (Kestrel's response
/// PipeWriter forbids concurrent writes), and a writer loop that dies must not leave a zombie
/// connection that hangs future senders.
/// </summary>
public class SseConnectionConcurrencyTests
{
    // Detects overlapping writes/flushes on the response body.
    private sealed class ConcurrencyDetectingStream : Stream
    {
        private int _active;
        public volatile bool ConcurrentDetected;

        private async Task GuardAsync(CancellationToken ct)
        {
            if (Interlocked.Increment(ref _active) > 1) ConcurrentDetected = true;
            try { await Task.Delay(3, ct); }
            finally { Interlocked.Decrement(ref _active); }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) => new(GuardAsync(ct));
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) => GuardAsync(ct);
        public override Task FlushAsync(CancellationToken ct) => GuardAsync(ct);
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set { } }
        public override void Flush() { }
        public override int Read(byte[] b, int o, int c) => throw new NotSupportedException();
        public override long Seek(long o, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long v) => throw new NotSupportedException();
        public override void Write(byte[] b, int o, int c) { }
    }

    // Fails every write, simulating a client disconnect mid-send.
    private sealed class ThrowingStream : Stream
    {
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
            => ValueTask.FromException(new IOException("client gone"));
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
            => Task.FromException(new IOException("client gone"));
        public override Task FlushAsync(CancellationToken ct) => Task.CompletedTask;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set { } }
        public override void Flush() { }
        public override int Read(byte[] b, int o, int c) => throw new NotSupportedException();
        public override long Seek(long o, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long v) => throw new NotSupportedException();
        public override void Write(byte[] b, int o, int c) => throw new IOException("client gone");
    }

    [Fact]
    public async Task Stream_SerializesWrites_UnderConcurrentEventsAndKeepAlives()
    {
        var body = new ConcurrencyDetectingStream();
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = body;
        await using var sse = new ServerSentEventStream(ctx.Response, CancellationToken.None);

        var tasks = new List<Task>();
        for (var i = 0; i < 20; i++)
        {
            tasks.Add(sse.SendEventAsync("evt", "<div>hi</div>"));
            tasks.Add(sse.SendKeepAliveAsync());
        }

        await Task.WhenAll(tasks);

        Assert.False(body.ConcurrentDetected,
            "SSE writes/flushes must be serialized; concurrent writes corrupt the response stream.");
    }

    [Fact]
    public async Task WriterLoop_MarksConnectionInactive_AndDoesNotHang_WhenSendFails()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new ThrowingStream();
        var stream = new ServerSentEventStream(ctx.Response, CancellationToken.None);
        await using var conn = new SseConnection("c1", stream, ctx);
        conn.ConfigureBroadcastOptions(new SseBroadcastOptions { MaxQueuedEventsPerConnection = 10 }, null);

        // First send drives the writer loop, whose write throws -> loop exits.
        var first = conn.SendEventAsync("evt", "<div>x</div>");
        try { await first.WaitAsync(TimeSpan.FromSeconds(2)); } catch { /* faulted/canceled is fine */ }

        // The dead loop must have cancelled the connection, not left it "active".
        for (var i = 0; i < 100 && conn.IsActive; i++) await Task.Delay(20);
        Assert.False(conn.IsActive, "a dead writer loop must mark the connection inactive");

        // A subsequent send must return promptly (no zombie hang), because the connection is cancelled.
        var second = conn.SendEventAsync("evt2", "<div>y</div>");
        var completed = await Task.WhenAny(second, Task.Delay(2000)) == second;
        Assert.True(completed, "sends to a dead connection must not hang");
    }
}
