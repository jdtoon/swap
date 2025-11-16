# Swap

HTMX + ASP.NET Core MVC, but ergonomic.

Swap is a small set of libraries that make it pleasant to build server‑rendered apps with HTMX and MVC:

- `Swap.Htmx` – runtime helpers: HTMX‑aware base controller, middleware, SSE primitives, event system, and extension methods for working with HX headers.
- `Swap.Testing` – testing helpers: HTMX‑aware integration test client and rich HTML/HTMX assertions.

You keep normal ASP.NET Core MVC. Swap gives you better defaults for HTMX requests and tests.

## Why Swap?

- **Stay server‑side** – HTML over the wire, no SPA framework required.
- **HTMX‑first MVC** – controllers and middleware that understand HX headers out of the box.
- **Events, not glue** – a small event system that turns server actions into `HX-Trigger` headers and optional SSE broadcasts.
- **Strong testing story** – integration tests that speak in terms of partials, HTMX attributes, and HX headers.

## Tiny example

`Program.cs` (minimal wiring):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SwapEventBusOptions>();
builder.Services.AddScoped<ISwapEventBus, SwapEventBus>();

var app = builder.Build();

app.UseSwapHtmxVary();
app.UseMiddleware<SwapEventResponseMiddleware>();

app.MapControllers();

app.Run();
```

`TodosController.cs`:

```csharp
public class TodosController : SwapController
{
	[HttpGet("/todos")]
	public IActionResult Index()
		=> SwapView("Index", model: _service.GetTodos());

	[HttpPost("/todos")]
	public IActionResult Create(TodoInput input, [FromServices] ISwapEventBus events)
	{
		var todo = _service.Create(input);
		events.Emit(SwapEvents.Entity.Created("todo"));
		return SwapView("_Todo", todo); // partial for HTMX, full view otherwise
	}
}
```

`TodosTests.cs` (using `Swap.Testing`):

```csharp
public class TodosTests : IClassFixture<HtmxTestFixture<Program>>
{
	private readonly HtmxTestClient<Program> _client;

	public TodosTests(HtmxTestFixture<Program> fixture)
	{
		_client = fixture.Client;
	}

	[Fact]
	public async Task GetTodos_ReturnsPartialWithItems()
	{
		var response = await _client.HtmxGetAsync("/todos");

		await response
			.AssertSuccess()
			.AssertPartialViewAsync()
			.AssertElementCountAsync(".todo-item", expectedCount: 5);
	}
}
```

## Packages

- [`Swap.Htmx`](lib/Swap.Htmx/README.md) – add HTMX‑friendly building blocks to an existing ASP.NET Core MVC app.
- [`Swap.Testing`](lib/Swap.Testing/README.md) – write fluent integration tests for HTMX endpoints.

## Examples

- `Swap.Htmx.TestApp` – small demo application showing HTMX + MVC + Swap.
- `lib/Swap.Testing/EXAMPLE_TESTS.cs` – longer example test suite using most of the testing helpers.

