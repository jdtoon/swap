using System.Linq;
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
    public void ConcurrentTrackUntrack_KeepsCountAndListConsistent()
    {
        var presence = new InMemoryRealtimePresence();

        Parallel.For(0, 4000, i =>
        {
            var connectionId = $"conn-{i % 25}";
            var room = $"room-{i % 6}";
            switch (i % 3)
            {
                case 0: presence.Track(connectionId, room, $"user-{i % 25}"); break;
                case 1: presence.Untrack(connectionId, room); break;
                default: presence.UntrackAll(connectionId); break;
            }
        });

        // Invariant under concurrency: Count(room) == distinct entries List(room) returns, with no
        // duplicate connection ids. (A no-throw check alone would miss lost/orphaned entries.)
        for (var r = 0; r < 6; r++)
        {
            var room = $"room-{r}";
            var list = presence.List(room);
            Assert.Equal(presence.Count(room), list.Count);
            Assert.Equal(list.Count, list.Select(e => e.ConnectionId).Distinct().Count());
        }
    }

    [Fact]
    public void UntrackAll_LeavesNoOrphan_AcrossRooms()
    {
        // Reproduces the reviewed orphan scenario: a connection in two rooms must be fully removed.
        var presence = new InMemoryRealtimePresence();
        presence.Track("conn-1", "room-a", "user-1");
        presence.Track("conn-1", "room-b", "user-1");

        presence.UntrackAll("conn-1");

        Assert.Empty(presence.List("room-a"));
        Assert.Empty(presence.List("room-b"));
        Assert.Equal(0, presence.Count("room-a"));
        Assert.Equal(0, presence.Count("room-b"));
    }
}
