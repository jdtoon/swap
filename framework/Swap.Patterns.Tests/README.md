# Swap.Patterns.Tests

Comprehensive test suite for the Swap.Patterns library, ensuring production-quality reliability for all entity patterns.

## Test Coverage

This test suite provides **72 tests** across all 8 patterns with complete coverage of:
- ✅ Happy path scenarios
- ✅ Edge cases and boundary conditions
- ✅ Database persistence and query behavior
- ✅ Integration tests with EF Core InMemory
- ✅ Error handling and validation
- ✅ Multiple operation sequences

## Test Files

### TimestampableTests (5 tests)
Tests for automatic timestamp management on entity creation and updates.

**Coverage:**
- `SetsTimestampsOnInsert` - Verifies CreatedAt is set on insert
- `UpdatesTimestampOnModify` - Verifies UpdatedAt is set on modification
- `MultipleUpdates_IncrementUpdatedAt` - Tests repeated update scenarios
- `BulkInsert_SetsAllTimestamps` - Validates bulk operations
- `NoTimestampChange_WhenNoModification` - Ensures no change when no update occurs

### OrderableExtensionsTests (8 tests)
Tests for position management and entity reordering functionality.

**Coverage:**
- `GetNextPosition_ReturnsOne_WhenEmpty` - Initial position assignment
- `GetNextPosition_IncrementsFromMax` - Position increment logic
- `Reorder_MoveUp` - Move entity to earlier position
- `Reorder_MoveDown` - Move entity to later position
- `Reorder_NoOp_WhenSamePosition` - No change when reordering to current position
- `OrderByPosition_Ascending` - Sort by position ascending
- `OrderByPosition_Descending` - Sort by position descending
- `SwapPositions` - Swap positions between two entities

### PublishableTests (8 tests)
Tests for publish/draft workflow and scheduled publishing.

**Coverage:**
- `Publish_SetsPublishedAtToNow` - Sets timestamp on publish
- `PublishedQuery_OnlyReturnsPublished` - Query filter for published content
- `DraftsQuery_OnlyReturnsDrafts` - Query filter for drafts
- `Publish_WithCustomTimestamp` - Custom publish timestamp support
- `PublishedAfter` - Filter by publish date (after)
- `PublishedBefore` - Filter by publish date (before)
- `Published_ExcludesDrafts` - Exclusion tests
- `Drafts_ExcludesPublished` - Exclusion tests

### VersionableTests (9 tests)
Tests for automatic version counter management on entities.

**Coverage:**
- `SetsVersionOnInsert` - Version starts at 1 on creation
- `IncrementsVersionOnModify` - Version increments on update
- `QueryByVersion` - Filter entities by version number
- `MultipleUpdates_KeepsIncrementing` - Version increments across multiple updates
- `PresetVersion_IsRespected` - Manual version setting is honored
- `OrderByVersion_Ascending` - Sort by version ascending
- `OrderByVersion_Descending` - Sort by version descending
- `NoVersionChange_WhenNoModification` - Version unchanged when no update
- `BulkInsert_SetsAllVersions` - Bulk operation version handling

### VisibilityTests (14 tests)
Tests for visibility flags and time-based scheduling.

**Coverage:**
- `Show_SetsIsVisible` - Show entity method
- `Hide_ClearsIsVisible` - Hide entity method
- `ScheduleVisibility_SetsTimeWindow` - Schedule visibility time window
- `IsCurrentlyVisible_OnlyFlag` - Visibility based on flag only
- `IsCurrentlyVisible_OnlyStart` - Visibility with start time only
- `IsCurrentlyVisible_OnlyEnd` - Visibility with end time only
- `IsCurrentlyVisible_WithinWindow` - Visibility within time window
- `IsCurrentlyVisible_BeforeWindow` - Not visible before start
- `IsCurrentlyVisible_AfterWindow` - Not visible after end
- `CurrentlyVisible_Query` - Query currently visible entities
- `Scheduled_Query` - Query entities with future visibility
- `Expired_Query` - Query entities with past visibility
- `Hidden_Query` - Query hidden entities
- `Show_ClearsSchedule` - Showing entity clears schedule

### SoftDeleteTests (10 tests)
Tests for soft delete functionality and query filtering.

**Coverage:**
- `SoftDelete_SetsProperties` - Sets IsDeleted, DeletedAt, DeletedBy
- `Restore_ClearsProperties` - Clears soft delete fields
- `QueryFilter_ExcludesDeleted` - Default query excludes soft deleted
- `IncludeDeleted_ShowsAll` - Include deleted entities in query
- `OnlyDeleted_ShowsOnlyDeleted` - Query only deleted entities
- `SoftDelete_WithUser_TracksDeletedBy` - User tracking on delete
- `SoftDelete_WithoutUser_NullDeletedBy` - No user tracking when not provided
- `Find_RespectsQueryFilter` - Find method respects query filters
- `RestoreAndQuery` - Restore makes entity queryable again
- `MultipleDeletes_UpdatesTimestamp` - Delete timestamp updates

### SluggableTests (13 tests)
Tests for URL-friendly slug generation and collision handling.

**Coverage:**
- `GenerateSlug_CreatesBasicSlug` - Basic slug generation ("Hello World" → "hello-world")
- `GenerateSlug_RemovesSpecialChars` - Special character removal ("C# & .NET" → "c-net")
- `GenerateSlug_HandlesUnicode` - Unicode normalization ("Café München" → "cafe-munchen")
- `GenerateSlug_HandlesCollisions` - Adds -2, -3 suffix for collisions
- `GenerateSlug_MaxLength` - Truncates to max length
- `GenerateSlug_EmptyString_ReturnsEmpty` - Empty string handling
- `GenerateSlug_OnlySpecialChars_ReturnsEmpty` - Only special chars returns empty
- `GenerateSlug_UpdateExisting_MayCollide` - Update collision detection
- `GenerateSlug_Numbers` - Preserves numbers in slugs
- `GenerateSlug_MultipleSpaces` - Normalizes multiple spaces
- `GenerateSlug_CaseNormalization` - Lowercases all characters
- `GenerateSlug_Apostrophes` - Handles apostrophes
- `GenerateSlug_ConfigureIndexes` - EF Core index configuration

### AuditableTests (9 tests)
Tests for automatic audit tracking with user information.

**Coverage:**
- `SetsAuditFieldsOnInsert_WithUser` - Sets CreatedAt, CreatedBy on insert
- `SetsAuditFieldsOnInsert_WithoutUser` - Audit without user context
- `UpdatesAuditFieldsOnModify` - Sets UpdatedAt, UpdatedBy on modify
- `MultipleUpdates_TracksLatest` - Tracks latest user across multiple updates
- `BulkInsert_SetsAllAuditFields` - Bulk operation audit tracking
- `NoAuditChange_WhenNoModification` - No audit change when no update
- `UsesNameClaim_WhenNameIdentifierMissing` - Falls back to Name claim
- `UsesEmailClaim_WhenOthersMissing` - Falls back to Email claim
- `CreatedBy_NeverChanges` - CreatedBy is immutable after creation

## Test Infrastructure

### Test Isolation
Each test uses a unique EF Core InMemory database (via GUID suffix) to ensure complete isolation and deterministic behavior.

```csharp
var options = new DbContextOptionsBuilder<TestDbContext>()
    .UseInMemoryDatabase($"pattern-tests-{Guid.NewGuid()}")
    .Options;
```

### Shared Database Tests
For tests requiring cross-context persistence (e.g., audit tracking across users), a shared database name is used:

```csharp
var dbName = $"pattern-tests-{Guid.NewGuid()}";
using var db1 = CreateContext("user1", dbName);
// ... perform operations
using var db2 = CreateContext("user2", dbName);
// ... verify state persisted
```

### Auditable Testing Pattern
Auditable tests simulate authenticated users via `HttpContextAccessor` with proper claims:

```csharp
var httpContext = new DefaultHttpContext();
var identity = new ClaimsIdentity(new[]
{
    new Claim(ClaimTypes.NameIdentifier, "user123")
}, "TestAuth"); // authenticationType required for IsAuthenticated = true

httpContext.User = new ClaimsPrincipal(identity);
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test file
dotnet test --filter "FullyQualifiedName~SoftDeleteTests"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

## Test Results

All 72 tests pass successfully:

```
Test summary: total: 72, failed: 0, succeeded: 72, skipped: 0
```

## Contributing Tests

When adding new patterns or enhancing existing ones:

1. **Create comprehensive test file** covering:
   - Happy path scenarios
   - Edge cases (empty collections, null values, boundaries)
   - Database persistence tests
   - Query filter tests
   - Error handling

2. **Use deterministic test data** with unique database names per test

3. **Follow naming conventions**: `MethodName_Scenario_ExpectedBehavior`

4. **Document test coverage** in this README when adding new test files

## Dependencies

- `xunit` (2.9.2) - Test framework
- `Microsoft.EntityFrameworkCore.InMemory` (9.0.10) - In-memory database for testing
- `Microsoft.AspNetCore.Http` (2.2.2) - HTTP context for auditable tests
- `Microsoft.NET.Test.Sdk` (17.11.1) - Test SDK

## Test Quality Standards

✅ **Deterministic** - Tests produce same result every run  
✅ **Isolated** - Tests don't affect each other  
✅ **Fast** - Full suite completes in ~2 seconds  
✅ **Comprehensive** - All patterns have edge case coverage  
✅ **Maintainable** - Clear naming and focused assertions  
