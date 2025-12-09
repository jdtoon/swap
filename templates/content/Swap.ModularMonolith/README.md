# SwapModularMonolith

A modular monolith starter template powered by **Swap.Htmx** for building server-driven reactive web applications.

## Quick Start

```bash
# Restore LibMan packages (HTMX)
cd src
libman restore

# Create initial migration
dotnet ef migrations add InitialCreate

# Run the application
dotnet run
```

## Project Structure

```
SwapModularMonolith/
├── src/
│   ├── Controllers/           # Shared controllers
│   ├── Data/                  # Database context & configurations
│   ├── Infrastructure/        # Cross-cutting concerns
│   ├── Modules/               # Feature modules (self-contained)
│   │   └── Notes/             # Sample module
│   │       ├── Controllers/
│   │       ├── Entities/
│   │       ├── Events/
│   │       ├── Services/
│   │       └── Views/
│   ├── Views/                 # Shared views & layout
│   └── wwwroot/               # Static assets
├── tests/                     # Integration tests
└── docker-compose.yml
```

## Adding a New Module

1. Create module folder: `Modules/[ModuleName]/`
2. Add entity, service, controller, events, and views
3. Register in `Program.cs`: `builder.Services.Add[ModuleName]Module();`
4. Add event config in `Infrastructure/MvcExtensions.cs`
5. Add view mapping in `Infrastructure/ModuleViewLocationExpander.cs`
6. Add DbSet in `Data/AppDbContext.cs`

## Key Concepts

### Source Generators (Zero Magic Strings)

This template is pre-configured with source generators that create type-safe constants at compile time.

**Setup in `.csproj`:**
```xml
<ItemGroup>
  <AdditionalFiles Include="Views\**\*.cshtml" />
  <AdditionalFiles Include="Modules\**\Views\**\*.cshtml" />
</ItemGroup>
```

**Type-safe Events:**
```csharp
// Modules/Notes/Events/NotesEvents.cs
[SwapEventSource]
public static partial class NotesEvents
{
    public const string NotesListChanged = "notes.listChanged";
}
// Generated: NotesEvents.Notes.ListChanged → EventKey("notes.listChanged")

// Usage — type-safe, refactorable
return SwapEvent(NotesEvents.Notes.ListChanged, payload).Build();
```

**Auto-generated View & Element Constants:**
```csharp
// Auto-generated from your .cshtml files
builder.AlsoUpdate(SwapElements.NotesList, SwapViews.Notes.List, notes);
// Instead of magic strings: builder.AlsoUpdate("notes-list", "_List", notes);
```

Generated files are output to `obj/Generated/` — check there to see what's available.

### Server-Driven UI
All UI updates flow through server-rendered HTML partials. Use HTMX attributes for interactivity:
- `hx-get`/`hx-post` for requests
- `hx-target` for response destination
- `hx-trigger` for event-based updates

### Event System
Events trigger reactive updates across the UI:
```csharp
// Define events
[SwapEventSource]
public static partial class NotesEvents
{
    public const string NoteCreated = "note.created";
}

// Trigger from controller
return SwapResponse()
    .WithTrigger(NotesEvents.Note.Created)
    .Build();

// React in views
<div hx-get="/Notes/List" 
     hx-trigger="@NotesEvents.List.Changed from:body">
```

## Commands

```bash
# Development
dotnet run

# Add migration
dotnet ef migrations add [MigrationName]

# Apply migrations
dotnet ef database update

# Docker
docker-compose up -d
```

## Learn More

- [Swap.Htmx Documentation](https://github.com/jdtoon/swap)
- [HTMX Documentation](https://htmx.org)
