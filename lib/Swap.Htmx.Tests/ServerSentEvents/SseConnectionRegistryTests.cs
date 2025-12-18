using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Swap.Htmx.Realtime;
using System.Security.Claims;
using Xunit;

namespace Swap.Htmx.Tests.ServerSentEvents;

public class SseConnectionRegistryTests
{
    [Fact]
    public void RegisterConnection_DoesNotThrow()
    {
        // Arrange
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var backplane = new InMemorySseBackplane(new Mock<ILogger<InMemorySseBackplane>>().Object);
        var registry = new SseConnectionRegistry(backplane, logger);
        
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;
        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
        var connection = new SseConnection("test-id", stream, httpContext);

        // Act & Assert - should not throw
        registry.RegisterConnection(connection);
    }

    [Fact]
    public void UnregisterConnection_DoesNotThrowForNonexistentConnection()
    {
        // Arrange
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var backplane = new InMemorySseBackplane(new Mock<ILogger<InMemorySseBackplane>>().Object);
        var registry = new SseConnectionRegistry(backplane, logger);

        // Act & Assert - should not throw
        registry.UnregisterConnection("nonexistent-id");
    }

    [Fact]
    public async Task BroadcastAsync_SendsToAllConnections()
    {
        // Arrange
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var backplane = new InMemorySseBackplane(new Mock<ILogger<InMemorySseBackplane>>().Object);
        var registry = new SseConnectionRegistry(backplane, logger);
        
        var stream1 = new MemoryStream();
        var stream2 = new MemoryStream();
        
        var httpContext1 = new DefaultHttpContext();
        httpContext1.Response.Body = stream1;
        var sseStream1 = new ServerSentEventStream(httpContext1.Response, CancellationToken.None);
        var connection1 = new SseConnection("conn1", sseStream1, httpContext1);
        
        var httpContext2 = new DefaultHttpContext();
        httpContext2.Response.Body = stream2;
        var sseStream2 = new ServerSentEventStream(httpContext2.Response, CancellationToken.None);
        var connection2 = new SseConnection("conn2", sseStream2, httpContext2);
        
        registry.RegisterConnection(connection1);
        registry.RegisterConnection(connection2);

        // Act
        await registry.BroadcastAsync("test-event", "<div>Content</div>");

        // Assert
        stream1.Position = 0;
        var reader1 = new StreamReader(stream1);
        var output1 = await reader1.ReadToEndAsync();
        Assert.Contains("data: <div>Content</div>", output1);

        stream2.Position = 0;
        var reader2 = new StreamReader(stream2);
        var output2 = await reader2.ReadToEndAsync();
        Assert.Contains("data: <div>Content</div>", output2);
    }

    [Fact]
    public async Task BroadcastToRoomsAsync_SendsOnlyToConnectionsInRooms()
    {
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var backplane = new InMemorySseBackplane(new Mock<ILogger<InMemorySseBackplane>>().Object);
        var registry = new SseConnectionRegistry(backplane, logger);

        var streamInRoom = new MemoryStream();
        var streamOutOfRoom = new MemoryStream();

        var httpContext1 = new DefaultHttpContext();
        httpContext1.Response.Body = streamInRoom;
        var conn1 = new SseConnection("conn1", new ServerSentEventStream(httpContext1.Response, CancellationToken.None), httpContext1);
        conn1.JoinRoom("room-a");

        var httpContext2 = new DefaultHttpContext();
        httpContext2.Response.Body = streamOutOfRoom;
        var conn2 = new SseConnection("conn2", new ServerSentEventStream(httpContext2.Response, CancellationToken.None), httpContext2);
        conn2.JoinRoom("room-b");

        registry.RegisterConnection(conn1);
        registry.RegisterConnection(conn2);

        await registry.BroadcastToRoomsAsync("room-event", "<div>Room</div>", new[] { "room-a" });

        streamInRoom.Position = 0;
        var inRoomText = await new StreamReader(streamInRoom).ReadToEndAsync();
        Assert.Contains("event: room-event", inRoomText);
        Assert.Contains("data: <div>Room</div>", inRoomText);

        streamOutOfRoom.Position = 0;
        var outRoomText = await new StreamReader(streamOutOfRoom).ReadToEndAsync();
        Assert.DoesNotContain("event: room-event", outRoomText);
        Assert.DoesNotContain("data: <div>Room</div>", outRoomText);
    }

    [Fact]
    public async Task BroadcastToSubscribersAsync_SendsOnlyToSubscribedConnections()
    {
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var backplane = new InMemorySseBackplane(new Mock<ILogger<InMemorySseBackplane>>().Object);
        var registry = new SseConnectionRegistry(backplane, logger);

        var subscribedStream = new MemoryStream();
        var unsubscribedStream = new MemoryStream();

        var httpContext1 = new DefaultHttpContext();
        httpContext1.Response.Body = subscribedStream;
        var conn1 = new SseConnection("conn1", new ServerSentEventStream(httpContext1.Response, CancellationToken.None), httpContext1);
        conn1.SubscribeToEvent("subscribed-event");

        var httpContext2 = new DefaultHttpContext();
        httpContext2.Response.Body = unsubscribedStream;
        var conn2 = new SseConnection("conn2", new ServerSentEventStream(httpContext2.Response, CancellationToken.None), httpContext2);

        registry.RegisterConnection(conn1);
        registry.RegisterConnection(conn2);

        await registry.BroadcastToSubscribersAsync("subscribed-event", "<div>Sub</div>");

        subscribedStream.Position = 0;
        var subscribedText = await new StreamReader(subscribedStream).ReadToEndAsync();
        Assert.Contains("event: subscribed-event", subscribedText);
        Assert.Contains("data: <div>Sub</div>", subscribedText);

        unsubscribedStream.Position = 0;
        var unsubscribedText = await new StreamReader(unsubscribedStream).ReadToEndAsync();
        Assert.DoesNotContain("event: subscribed-event", unsubscribedText);
        Assert.DoesNotContain("data: <div>Sub</div>", unsubscribedText);
    }

    [Fact]
    public async Task BroadcastToRolesAsync_SendsOnlyToAuthenticatedUsersInRole()
    {
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var backplane = new InMemorySseBackplane(new Mock<ILogger<InMemorySseBackplane>>().Object);
        var registry = new SseConnectionRegistry(backplane, logger);

        var adminStream = new MemoryStream();
        var userStream = new MemoryStream();

        var adminContext = new DefaultHttpContext();
        adminContext.Response.Body = adminStream;
        adminContext.User = CreateAuthenticatedUser(name: "admin1", userId: "admin1", roles: new[] { "Admin" });
        var adminConn = new SseConnection("admin", new ServerSentEventStream(adminContext.Response, CancellationToken.None), adminContext);

        var userContext = new DefaultHttpContext();
        userContext.Response.Body = userStream;
        userContext.User = CreateAuthenticatedUser(name: "user1", userId: "user1", roles: new[] { "User" });
        var userConn = new SseConnection("user", new ServerSentEventStream(userContext.Response, CancellationToken.None), userContext);

        registry.RegisterConnection(adminConn);
        registry.RegisterConnection(userConn);

        await registry.BroadcastToRolesAsync("role-event", "<div>Role</div>", new[] { "Admin" });

        adminStream.Position = 0;
        var adminText = await new StreamReader(adminStream).ReadToEndAsync();
        Assert.Contains("event: role-event", adminText);
        Assert.Contains("data: <div>Role</div>", adminText);

        userStream.Position = 0;
        var userText = await new StreamReader(userStream).ReadToEndAsync();
        Assert.DoesNotContain("event: role-event", userText);
        Assert.DoesNotContain("data: <div>Role</div>", userText);
    }

    [Fact]
    public async Task BroadcastToUserAsync_SendsOnlyToMatchingAuthenticatedUser()
    {
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var backplane = new InMemorySseBackplane(new Mock<ILogger<InMemorySseBackplane>>().Object);
        var registry = new SseConnectionRegistry(backplane, logger);

        var matchingStream = new MemoryStream();
        var otherStream = new MemoryStream();

        var matchingContext = new DefaultHttpContext();
        matchingContext.Response.Body = matchingStream;
        matchingContext.User = CreateAuthenticatedUser(name: "user123", userId: "user123", roles: Array.Empty<string>());
        var matchingConn = new SseConnection("u1", new ServerSentEventStream(matchingContext.Response, CancellationToken.None), matchingContext);

        var otherContext = new DefaultHttpContext();
        otherContext.Response.Body = otherStream;
        otherContext.User = CreateAuthenticatedUser(name: "user999", userId: "user999", roles: Array.Empty<string>());
        var otherConn = new SseConnection("u2", new ServerSentEventStream(otherContext.Response, CancellationToken.None), otherContext);

        registry.RegisterConnection(matchingConn);
        registry.RegisterConnection(otherConn);

        await registry.BroadcastToUserAsync("user-event", "<div>User</div>", "user123");

        matchingStream.Position = 0;
        var matchingText = await new StreamReader(matchingStream).ReadToEndAsync();
        Assert.Contains("event: user-event", matchingText);
        Assert.Contains("data: <div>User</div>", matchingText);

        otherStream.Position = 0;
        var otherText = await new StreamReader(otherStream).ReadToEndAsync();
        Assert.DoesNotContain("event: user-event", otherText);
        Assert.DoesNotContain("data: <div>User</div>", otherText);
    }

    [Fact]
    public async Task BroadcastToFilteredAsync_SendsOnlyToConnectionsMatchingPredicate()
    {
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var backplane = new InMemorySseBackplane(new Mock<ILogger<InMemorySseBackplane>>().Object);
        var registry = new SseConnectionRegistry(backplane, logger);

        var stream1 = new MemoryStream();
        var stream2 = new MemoryStream();

        var context1 = new DefaultHttpContext();
        context1.Response.Body = stream1;
        var conn1 = new SseConnection("conn1", new ServerSentEventStream(context1.Response, CancellationToken.None), context1);
        conn1.JoinRoom("monitoring");

        var context2 = new DefaultHttpContext();
        context2.Response.Body = stream2;
        var conn2 = new SseConnection("conn2", new ServerSentEventStream(context2.Response, CancellationToken.None), context2);
        conn2.JoinRoom("other");

        registry.RegisterConnection(conn1);
        registry.RegisterConnection(conn2);

        await registry.BroadcastToFilteredAsync("filtered-event", "<div>Filtered</div>", c => c.IsInRoom("monitoring"));

        stream1.Position = 0;
        var text1 = await new StreamReader(stream1).ReadToEndAsync();
        Assert.Contains("event: filtered-event", text1);
        Assert.Contains("data: <div>Filtered</div>", text1);

        stream2.Position = 0;
        var text2 = await new StreamReader(stream2).ReadToEndAsync();
        Assert.DoesNotContain("event: filtered-event", text2);
        Assert.DoesNotContain("data: <div>Filtered</div>", text2);
    }

    private static ClaimsPrincipal CreateAuthenticatedUser(string name, string userId, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new("sub", userId),
            new(ClaimTypes.Name, name),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
