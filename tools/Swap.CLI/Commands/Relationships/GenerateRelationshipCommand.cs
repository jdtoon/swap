using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using Swap.CLI.Commands.Relationships.Models;

namespace Swap.CLI.Commands.Relationships;

/// <summary>
/// Command to generate relationships between entities
/// Usage: swap generate relationship --source Order --target Customer --type one-to-many
/// </summary>
public static class GenerateRelationshipCommand
{
    public static Command Create()
    {
        var command = new Command("relationship", "Generate a relationship between two entities");
        command.AddAlias("rel");

        // Required options
        var sourceOption = new Option<string?>(
            aliases: new[] { "--source", "-s" },
            description: "The source entity (e.g., Order)");

        var targetOption = new Option<string?>(
            aliases: new[] { "--target", "-t" },
            description: "The target entity (e.g., Customer)");

        var typeOption = new Option<string?>(
            "--type",
            description: "Relationship type: one-to-many, many-to-one, many-to-many, one-to-one");

        // Configuration options
        var requiredOption = new Option<bool>(
            "--required",
            description: "Make the foreign key required (NOT NULL)");

        var onDeleteOption = new Option<string>(
            "--on-delete",
            () => "restrict",
            description: "Delete behavior: cascade, restrict, set-null");

        var displayOption = new Option<string?>(
            "--display",
            description: "Field to display in dropdowns (e.g., Name, Title)");

        var fkOption = new Option<string?>(
            "--fk",
            description: "Foreign key property name (e.g., CustomerId)");

        var navOption = new Option<string?>(
            "--nav",
            description: "Navigation property name");

        var inverseOption = new Option<string?>(
            "--inverse",
            description: "Inverse navigation property name");

        // Many-to-many options
        var junctionOption = new Option<string?>(
            "--junction",
            description: "Junction table name (for many-to-many)");

        var junctionPropsOption = new Option<string?>(
            "--junction-props",
            description: "Additional junction table properties (comma-separated: CreatedAt:datetime,CreatedBy:string)");

        // One-to-one options
        var principalOption = new Option<string?>(
            "--principal",
            description: "Principal entity (for one-to-one)");

        var dependentOption = new Option<string?>(
            "--dependent",
            description: "Dependent entity (for one-to-one)");

        // Skip options
        var skipNavOption = new Option<bool>(
            "--skip-nav",
            description: "Skip navigation property generation");

        var skipUIOption = new Option<bool>(
            "--skip-ui",
            description: "Skip UI generation");

        var skipMigrationsOption = new Option<bool>(
            "--skip-migrations",
            description: "Skip automatic migration creation");

        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");

        // Add all options
        command.AddOption(sourceOption);
        command.AddOption(targetOption);
        command.AddOption(typeOption);
        command.AddOption(requiredOption);
        command.AddOption(onDeleteOption);
        command.AddOption(displayOption);
        command.AddOption(fkOption);
        command.AddOption(navOption);
        command.AddOption(inverseOption);
        command.AddOption(junctionOption);
        command.AddOption(junctionPropsOption);
        command.AddOption(principalOption);
        command.AddOption(dependentOption);
        command.AddOption(skipNavOption);
        command.AddOption(skipUIOption);
        command.AddOption(skipMigrationsOption);
        command.AddOption(projectOption);

        command.SetHandler((InvocationContext context) =>
        {
            var source = context.ParseResult.GetValueForOption(sourceOption);
            var target = context.ParseResult.GetValueForOption(targetOption);
            var type = context.ParseResult.GetValueForOption(typeOption);
            var required = context.ParseResult.GetValueForOption(requiredOption);
            var onDelete = context.ParseResult.GetValueForOption(onDeleteOption) ?? "restrict";
            var display = context.ParseResult.GetValueForOption(displayOption);
            var fk = context.ParseResult.GetValueForOption(fkOption);
            var nav = context.ParseResult.GetValueForOption(navOption);
            var inverse = context.ParseResult.GetValueForOption(inverseOption);
            var junction = context.ParseResult.GetValueForOption(junctionOption);
            var junctionProps = context.ParseResult.GetValueForOption(junctionPropsOption);
            var principal = context.ParseResult.GetValueForOption(principalOption);
            var dependent = context.ParseResult.GetValueForOption(dependentOption);
            var skipNav = context.ParseResult.GetValueForOption(skipNavOption);
            var skipUI = context.ParseResult.GetValueForOption(skipUIOption);
            var skipMigrations = context.ParseResult.GetValueForOption(skipMigrationsOption);
            var project = context.ParseResult.GetValueForOption(projectOption);

            context.ExitCode = Execute(
                source, target, type, required, onDelete, display, fk, nav, inverse,
                junction, junctionProps, principal, dependent,
                skipNav, skipUI, skipMigrations, project);
        });

        return command;
    }

    private static int Execute(
        string? source, string? target, string? type, bool required, string onDelete,
        string? display, string? fk, string? nav, string? inverse,
        string? junction, string? junctionProps, string? principal, string? dependent,
        bool skipNav, bool skipUI, bool skipMigrations, string? projectPath)
    {
        try
        {
            AnsiConsole.Write(new FigletText("Swap CLI").Color(Color.Blue));
            AnsiConsole.MarkupLine("[bold blue]Generate Relationship[/]");
            AnsiConsole.WriteLine();

            // Build relationship definition
            var definition = BuildDefinition(
                source, target, type, required, onDelete, display, fk, nav, inverse,
                junction, junctionProps, principal, dependent,
                skipNav, skipUI, skipMigrations, projectPath);

            // Validate the relationship definition
            var validator = new RelationshipValidator();
            var validationResult = validator.Validate(definition);

            if (!validationResult.IsValid)
            {
                AnsiConsole.MarkupLine("[red]Validation failed:[/]");
                foreach (var error in validationResult.Errors)
                {
                    AnsiConsole.MarkupLine($"[red]  • {error}[/]");
                }
                return 1;
            }

            // TODO: Phase 1 - Just validate, don't generate yet
            AnsiConsole.MarkupLine("[green]✓ Relationship definition is valid[/]");
            AnsiConsole.MarkupLine($"[dim]Source:[/] {definition.SourceEntity}");
            AnsiConsole.MarkupLine($"[dim]Target:[/] {definition.TargetEntity}");
            AnsiConsole.MarkupLine($"[dim]Type:[/] {definition.Type}");
            AnsiConsole.MarkupLine($"[dim]On Delete:[/] {definition.OnDelete}");
            AnsiConsole.MarkupLine($"[dim]Required:[/] {definition.IsRequired}");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Note: Code generation not yet implemented (Phase 2+)[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private static RelationshipDefinition BuildDefinition(
        string? source, string? target, string? type, bool required, string onDelete,
        string? display, string? fk, string? nav, string? inverse,
        string? junction, string? junctionProps, string? principal, string? dependent,
        bool skipNav, bool skipUI, bool skipMigrations, string? projectPath)
    {
        var definition = new RelationshipDefinition
        {
            SourceEntity = source ?? string.Empty,
            TargetEntity = target ?? string.Empty,
            IsRequired = required,
            DisplayField = display,
            ForeignKeyName = fk,
            NavigationProperty = nav,
            InverseNavigation = inverse,
            JunctionTable = junction,
            PrincipalEntity = principal,
            DependentEntity = dependent,
            SkipNavigation = skipNav,
            SkipUI = skipUI,
            SkipMigrations = skipMigrations,
            ProjectPath = projectPath ?? Directory.GetCurrentDirectory()
        };

        // Parse relationship type
        if (!string.IsNullOrWhiteSpace(type))
        {
            definition.Type = type.ToLowerInvariant() switch
            {
                "one-to-many" or "onetomany" or "1:n" => RelationshipType.OneToMany,
                "many-to-one" or "manytoone" or "n:1" => RelationshipType.ManyToOne,
                "many-to-many" or "manytomany" or "n:n" => RelationshipType.ManyToMany,
                "one-to-one" or "onetoone" or "1:1" => RelationshipType.OneToOne,
                _ => throw new InvalidOperationException($"Invalid relationship type: {type}")
            };
        }

        // Parse delete behavior
        definition.OnDelete = onDelete.ToLowerInvariant() switch
        {
            "cascade" => DeleteBehavior.Cascade,
            "restrict" => DeleteBehavior.Restrict,
            "set-null" or "setnull" => DeleteBehavior.SetNull,
            _ => throw new InvalidOperationException($"Invalid delete behavior: {onDelete}")
        };

        // Parse junction properties if provided
        if (!string.IsNullOrWhiteSpace(junctionProps))
        {
            definition.JunctionProperties = new Dictionary<string, string>();
            var props = junctionProps.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var prop in props)
            {
                var parts = prop.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    definition.JunctionProperties[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        return definition;
    }
}
