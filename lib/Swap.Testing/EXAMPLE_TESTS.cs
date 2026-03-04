using System.Net;
using Swap.Testing;
using Xunit;

namespace MyApp.Tests;

/// <summary>
/// Example test class demonstrating Swap.Testing usage.
/// Copy this file to your test project and replace 'Program' with your app's entry point.
/// </summary>
public class ExampleControllerTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public ExampleControllerTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    // ============================================================================
    // Basic HTTP Tests
    // ============================================================================

    [Fact]
    public async Task GetHomePage_ReturnsSuccessAndWelcomeMessage()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        await response
            .AssertSuccess()
            .AssertContainsAsync("Welcome");
    }

    [Fact]
    public async Task GetNonExistentPage_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/does-not-exist");

        // Assert
        response.AssertStatus(HttpStatusCode.NotFound);
    }

    // ============================================================================
    // HTMX Partial View Tests
    // ============================================================================

    [Fact]
    public async Task GetTodoEditPartial_IsPartialWithHtmxAttributes()
    {
        // Act - Make HTMX request for edit form
        var response = await _client.HtmxGetAsync("/todos/1/edit");

        // Assert - Verify partial view structure
        await response
            .AssertSuccess()
            .AssertPartialViewAsync() // No <html> or <body> tags
            .AssertElementExistsAsync("form")
            .AssertHxPostAsync("form", "/todos/1") // Form posts to update endpoint
            .AssertHxTargetAsync("form", "#todo-1") // Replaces todo item
            .AssertHxSwapAsync("form", "outerHTML"); // Swaps entire element
    }

    [Fact]
    public async Task GetTodoList_DisplaysAllTodos()
    {
        // Act
        var response = await _client.HtmxGetAsync("/todos");

        // Assert - Check HTML structure
        await response
            .AssertSuccess()
            .AssertElementCountAsync(".todo-item", 5) // 5 todos
            .AssertElementExistsAsync("#add-todo-button")
            .AssertHxGetAsync(".edit-button:first-child", "/todos/1/edit");
    }

    // ============================================================================
    // POST/PUT/DELETE Tests
    // ============================================================================

    [Fact]
    public async Task CreateTodo_WithValidData_ReturnsNewTodoPartial()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Buy groceries",
            ["description"] = "Milk, eggs, bread",
            ["completed"] = "false"
        };

        // Act
        var response = await _client.HtmxPostAsync("/todos", formData);

        // Assert
        await response
            .AssertStatus(HttpStatusCode.Created)
            .AssertPartialViewAsync()
            .AssertContainsAsync("Buy groceries")
            .AssertElementExistsAsync(".todo-item");
    }

    [Fact]
    public async Task UpdateTodo_WithValidData_ReturnsUpdatedPartial()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Updated title",
            ["completed"] = "true"
        };

        // Act
        var response = await _client.HtmxPutAsync("/todos/1", formData);

        // Assert
        await response
            .AssertSuccess()
            .AssertPartialViewAsync()
            .AssertContainsAsync("Updated title")
            .AssertElementExistsAsync(".completed");
    }

    [Fact]
    public async Task DeleteTodo_ReturnsEmptyElement()
    {
        // Act
        var response = await _client.HtmxDeleteAsync("/todos/1");

        // Assert
        await response
            .AssertSuccess()
            .AssertContainsAsync("<div id=\"todo-1\"></div>"); // Empty replacement
    }

    // ============================================================================
    // HTMX Headers and Triggers
    // ============================================================================

    [Fact]
    public async Task CreateTodo_TriggersCustomEvent()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["title"] = "New todo"
        };

        // Act
        var response = await _client.HtmxPostAsync("/todos", formData);

        // Assert - Check for HX-Trigger response header
        response
            .AssertSuccess()
            .AssertHeader("HX-Trigger", "todoCreated");
    }

    [Fact]
    public async Task GetTodoDetails_WithSpecificTarget_ReturnsCorrectPartial()
    {
        // Act - Simulate clicking element with hx-target="#details"
        var response = await _client.HtmxGetAsync(
            path: "/todos/1",
            target: "#details",
            trigger: "click from:#todo-1"
        );

        // Assert
        await response
            .AssertSuccess()
            .AssertPartialViewAsync()
            .AssertElementExistsAsync(".todo-details");
    }

    // ============================================================================
    // Complex Assertions
    // ============================================================================

    [Fact]
    public async Task GetTodoList_HasCorrectHtmxStructure()
    {
        // Act
        var response = await _client.HtmxGetAsync("/todos");

        // Assert - Custom assertion logic
        await response.AssertAsync(async doc =>
        {
            var todos = doc.QuerySelectorAll(".todo-item");
            
            Assert.NotEmpty(todos);

            foreach (var todo in todos)
            {
                // Each todo should have title
                var title = todo.QuerySelector(".todo-title");
                Assert.NotNull(title);
                Assert.False(string.IsNullOrEmpty(title.TextContent));

                // Each todo should have edit button with hx-get
                var editBtn = todo.QuerySelector("button[hx-get]");
                Assert.NotNull(editBtn);
                Assert.Contains("/edit", editBtn.GetAttribute("hx-get"));

                // Each todo should have delete button with hx-delete
                var deleteBtn = todo.QuerySelector("button[hx-delete]");
                Assert.NotNull(deleteBtn);
                Assert.Contains("/todos/", deleteBtn.GetAttribute("hx-delete"));
            }
        });
    }

    [Fact]
    public async Task SearchTodos_WithQuery_FiltersResults()
    {
        // Act
        var response = await _client.HtmxGetAsync("/todos?search=groceries");

        // Assert
        await response
            .AssertSuccess()
            .AssertContainsAsync("groceries")
            .AssertDoesNotContainAsync("unrelated-todo");
    }

    // ============================================================================
    // Form Validation Tests
    // ============================================================================

    [Fact]
    public async Task CreateTodo_WithInvalidData_ReturnsValidationErrors()
    {
        // Arrange - Empty title (invalid)
        var formData = new Dictionary<string, string>
        {
            ["title"] = "",
            ["description"] = "No title"
        };

        // Act
        var response = await _client.HtmxPostAsync("/todos", formData);

        // Assert
        await response
            .AssertStatus(HttpStatusCode.BadRequest)
            .AssertContainsAsync("validation-error")
            .AssertElementExistsAsync(".field-validation-error");
    }

    // ============================================================================
    // Testing with Custom Headers
    // ============================================================================

    [Fact]
    public async Task GetUserData_WithAuthHeader_ReturnsUserInfo()
    {
        // Act
        var response = await _client
            .WithHeader("Authorization", "Bearer test-token")
            .GetAsync("/user/profile");

        // Assert
        await response
            .AssertSuccess()
            .AssertContainsAsync("User Profile");
    }

    [Fact]
    public async Task HtmxRequest_WithMultipleHeaders_SendsAllHeaders()
    {
        // Act
        var response = await _client
            .WithHeader("X-Custom-Header", "custom-value")
            .AsHtmxRequest()
            .GetAsync("/todos");

        // Assert - Just verify request succeeds
        response.AssertSuccess();
    }

    // ============================================================================
    // Snapshot Testing
    // ============================================================================

    [Fact]
    public async Task GetTodoList_MatchesSnapshot()
    {
        // Act
        var response = await _client.HtmxGetAsync("/todos");

        // Assert - Compare against saved snapshot
        // First run creates snapshot, subsequent runs compare
        // Set UPDATE_SNAPSHOTS=true env var to update snapshots
        await response
            .AssertSuccess()
            .AssertMatchesSnapshotAsync("todo-list-partial");
    }

    [Fact]
    public async Task GetTodoEditForm_MatchesSnapshot()
    {
        // Act
        var response = await _client.HtmxGetAsync("/todos/1/edit");

        // Assert - Snapshot with custom directory
        await response
            .AssertSuccess()
            .AssertMatchesSnapshotAsync(
                "todo-edit-form",
                snapshotDirectory: "__test_snapshots__");
    }

    // ============================================================================
    // OOB Swap Introspection (v2)
    // ============================================================================

    [Fact]
    public async Task PurchaseItem_UpdatesMultipleOobSections()
    {
        // Act - Purchase that triggers OOB updates for cart, total, inventory
        var response = await _client.HtmxPostAsync("/shop/purchase",
            new Dictionary<string, string> { ["itemId"] = "42" });

        // Assert - Verify all OOB swaps are present
        var swaps = await response.GetOobSwapsAsync();
        Assert.True(swaps.Count >= 3); // At least cart, total, inventory

        // Assert specific OOB targets
        await response
            .AssertSuccess()
            .AssertOobSwapExistsAsync("cart-count")
            .AssertOobSwapExistsAsync("cart-total")
            .AssertOobSwapContentAsync("cart-count", "1")
            .AssertOobSwapCountAsync(3);
    }

    // ============================================================================
    // Trigger Payload Assertions (v2)
    // ============================================================================

    [Fact]
    public async Task CreateTodo_AssertTriggerPayload()
    {
        // Act
        var response = await _client.HtmxPostAsync("/todos",
            new Dictionary<string, string> { ["title"] = "New task" });

        // Assert - Check trigger event and nested payload
        response
            .AssertTrigger("showToast")
            .AssertTriggerPayload("showToast", "message", "Created")
            .AssertTriggerPayload("showToast", "type", "success")
            .AssertTriggerCount(1);

        // Or deserialize the entire payload
        var toast = response.GetTriggerPayload<ToastData>("showToast");
        Assert.Equal("Created", toast!.Message);
    }

    // ============================================================================
    // Form Field Assertions (v2)
    // ============================================================================

    [Fact]
    public async Task GetEditForm_HasCorrectFieldValues()
    {
        // Act
        var response = await _client.HtmxGetAsync("/todos/1/edit");

        // Assert - Check form fields exist and have correct values
        await response
            .AssertSuccess()
            .AssertFormFieldExistsAsync("title")
            .AssertFormFieldExistsAsync("description")
            .AssertFormFieldExistsAsync("completed")
            .AssertFormValueAsync("title", "Buy groceries")
            .AssertFormValueAsync("completed", "true");
    }

    // ============================================================================
    // Cookie Persistence (v2)
    // ============================================================================

    [Fact]
    public async Task Login_PersistsCookiesAcrossRequests()
    {
        // Step 1: Login sets a session cookie
        var loginResponse = await _client.HtmxPostAsync("/auth/login",
            new Dictionary<string, string>
            {
                ["username"] = "admin",
                ["password"] = "test123"
            });
        loginResponse.AssertSuccess();

        // Step 2: Cookies persist — next request is authenticated
        var dashboardResponse = await _client.GetAsync("/dashboard");
        await dashboardResponse
            .AssertSuccess()
            .AssertContainsAsync("Welcome, admin");

        // Step 3: Inspect cookies
        var cookies = _client.Cookies.GetAllCookies();
        Assert.True(cookies.Count > 0);

        // Step 4: Clear cookies and verify re-authentication required
        _client.ClearCookies();
        var afterClear = await _client.GetAsync("/dashboard");
        afterClear.AssertStatus(HttpStatusCode.Redirect);
    }

    // ============================================================================
    // Snapshot Scrubbers (v2)
    // ============================================================================

    [Fact]
    public async Task GetPage_MatchesSnapshotWithCustomScrubbers()
    {
        // Add scrubbers for dynamic content before comparing
        SnapshotManager.ScrubUrls(); // Replace URLs with [URL]
        SnapshotManager.ScrubRegex(@"tenant-\w+", "[TENANT]"); // Replace tenant slugs

        try
        {
            var response = await _client.HtmxGetAsync("/todos");
            await response
                .AssertSuccess()
                .AssertMatchesSnapshotAsync("todo-list-scrubbed");
        }
        finally
        {
            SnapshotManager.ClearScrubbers(); // Clean up
        }
    }

    // Helper class for typed trigger deserialization
    private sealed class ToastData
    {
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
