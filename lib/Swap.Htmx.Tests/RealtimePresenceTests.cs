using System.Threading.Tasks;
using Swap.Htmx.Realtime;
using Xunit;

namespace Swap.Htmx.Tests;

public class RealtimePresenceTests
{
    [Fact]
    public void Track_ShouldAddEntryToRoom()
    {
        // Arrange
        var presence = new InMemoryRealtimePresence();

        // Act
        presence.Track("conn-1", "room-a", "user-1");

        // Assert
        var entries = presence.List("room-a");
        Assert.Single(entries);
        Assert.Equal("conn-1", entries[0].ConnectionId);
        Assert.Equal("user-1", entries[0].UserId);
        Assert.Equal("room-a", entries[0].Room);
    }

    [Fact]
    public void List_ShouldOnlyReturnEntriesForRequestedRoom()
    {
        // Arrange
        var presence = new InMemoryRealtimePresence();
        presence.Track("conn-1", "room-a", "user-1");
        presence.Track("conn-2", "room-b", "user-2");

        // Act
        var roomAEntries = presence.List("room-a");

        // Assert
        Assert.Single(roomAEntries);
        Assert.Equal("conn-1", roomAEntries[0].ConnectionId);
    }

    [Fact]
    public void Untrack_ShouldRemoveOnlySpecifiedRoomEntry()
    {
        // Arrange
        var presence = new InMemoryRealtimePresence();
        presence.Track("conn-1", "room-a", "user-1");
        presence.Track("conn-1", "room-b", "user-1");

        // Act
        presence.Untrack("conn-1", "room-a");

        // Assert
        Assert.Empty(presence.List("room-a"));
        Assert.Single(presence.List("room-b"));
    }

    [Fact]
    public void UntrackAll_ShouldRemoveConnectionFromAllRooms()
    {
        // Arrange
        var presence = new InMemoryRealtimePresence();
        presence.Track("conn-1", "room-a", "user-1");
        presence.Track("conn-1", "room-b", "user-1");
        presence.Track("conn-2", "room-a", "user-2");

        // Act
        presence.UntrackAll("conn-1");

        // Assert
        Assert.Empty(presence.List("room-b"));
        var roomAEntries = presence.List("room-a");
        Assert.Single(roomAEntries);
        Assert.Equal("conn-2", roomAEntries[0].ConnectionId);
    }

    [Fact]
    public void Count_ShouldReturnDistinctConnectionCount()
    {
        // Arrange
        var presence = new InMemoryRealtimePresence();
        presence.Track("conn-1", "room-a", "user-1");
        presence.Track("conn-1", "room-a", "user-1");
        presence.Track("conn-2", "room-a", "user-2");

        // Act & Assert
        Assert.Equal(2, presence.Count("room-a"));
    }

    [Fact]
    public void ConcurrentTrackAndUntrack_ShouldNotThrow()
    {
        // Arrange
        var presence = new InMemoryRealtimePresence();

        // Act
        var exception = Record.Exception(() =>
        {
            Parallel.For(0, 200, i =>
            {
                var connectionId = $"conn-{i % 20}";
                var room = $"room-{i % 5}";
                presence.Track(connectionId, room, $"user-{i}");
                presence.List(room);
                presence.Count(room);
                if (i % 2 == 0)
                {
                    presence.Untrack(connectionId, room);
                }
            });
        });

        // Assert
        Assert.Null(exception);
    }
}
