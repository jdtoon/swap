using Swap.CLI.Infrastructure;
using Xunit;

namespace Swap.CLI.Tests.Infrastructure;

public class TemplateEngineTests
{
    [Fact]
    public void Process_ShouldReplaceSimpleVariables()
    {
        // Arrange
        var template = "Hello {{Name}}, welcome to {{ProjectName}}!";
        var variables = new Dictionary<string, string>
        {
            { "Name", "World" },
            { "ProjectName", "MyApp" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Equal("Hello World, welcome to MyApp!", result);
    }

    [Fact]
    public void Process_ShouldHandleMultipleOccurrencesOfSameVariable()
    {
        // Arrange
        var template = "{{Name}} said {{Name}} again";
        var variables = new Dictionary<string, string>
        {
            { "Name", "Echo" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Equal("Echo said Echo again", result);
    }

    [Fact]
    public void Process_ShouldIncludeContentWhenConditionIsTrue_Sqlite()
    {
        // Arrange
        var template = @"
{{#if_sqlite}}
options.UseSqlite(connectionString);
{{/if_sqlite}}
{{#if_sqlserver}}
options.UseSqlServer(connectionString);
{{/if_sqlserver}}
{{#if_postgres}}
options.UseNpgsql(connectionString);
{{/if_postgres}}";
        var variables = new Dictionary<string, string>
        {
            { "DatabaseProvider", "sqlite" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("UseSqlite", result);
        Assert.DoesNotContain("UseSqlServer", result);
        Assert.DoesNotContain("UseNpgsql", result);
    }

    [Fact]
    public void Process_ShouldIncludeContentWhenConditionIsTrue_SqlServer()
    {
        // Arrange
        var template = @"
{{#if_sqlite}}
options.UseSqlite(connectionString);
{{/if_sqlite}}
{{#if_sqlserver}}
options.UseSqlServer(connectionString);
{{/if_sqlserver}}
{{#if_postgres}}
options.UseNpgsql(connectionString);
{{/if_postgres}}";
        var variables = new Dictionary<string, string>
        {
            { "DatabaseProvider", "sqlserver" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.DoesNotContain("UseSqlite", result);
        Assert.Contains("UseSqlServer", result);
        Assert.DoesNotContain("UseNpgsql", result);
    }

    [Fact]
    public void Process_ShouldIncludeContentWhenConditionIsTrue_Postgres()
    {
        // Arrange
        var template = @"
{{#if_sqlite}}
options.UseSqlite(connectionString);
{{/if_sqlite}}
{{#if_sqlserver}}
options.UseSqlServer(connectionString);
{{/if_sqlserver}}
{{#if_postgres}}
options.UseNpgsql(connectionString);
{{/if_postgres}}";
        var variables = new Dictionary<string, string>
        {
            { "DatabaseProvider", "postgres" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.DoesNotContain("UseSqlite", result);
        Assert.DoesNotContain("UseSqlServer", result);
        Assert.Contains("UseNpgsql", result);
    }

    [Fact]
    public void Process_ShouldHandleNestedVariablesInConditionalBlocks()
    {
        // Arrange
        var template = @"
{{#if_sqlite}}
Database: {{DatabaseProvider}}
Project: {{ProjectName}}
{{/if_sqlite}}";
        var variables = new Dictionary<string, string>
        {
            { "DatabaseProvider", "sqlite" },
            { "ProjectName", "TestApp" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("Database: sqlite", result);
        Assert.Contains("Project: TestApp", result);
    }

    [Fact]
    public void Process_ShouldHandleEmptyTemplate()
    {
        // Arrange
        var template = "";
        var variables = new Dictionary<string, string>();

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Process_ShouldHandleTemplateWithNoVariables()
    {
        // Arrange
        var template = "Plain text with no variables";
        var variables = new Dictionary<string, string>();

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Equal("Plain text with no variables", result);
    }

    [Fact]
    public void Process_ShouldPreserveWhitespaceAndNewlines()
    {
        // Arrange
        var template = @"Line 1
    Indented Line 2
        Double Indented {{Variable}}";
        var variables = new Dictionary<string, string>
        {
            { "Variable", "Value" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert - Use Environment.NewLine for platform independence
        Assert.Contains("Line 1", result);
        Assert.Contains("    Indented Line 2", result);
        Assert.Contains("        Double Indented Value", result);
    }

    [Fact]
    public void Process_ShouldHandleMissingVariables_LeavesPlaceholder()
    {
        // Arrange
        var template = "Hello {{Name}}, {{Missing}} variable";
        var variables = new Dictionary<string, string>
        {
            { "Name", "World" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("Hello World", result);
        Assert.Contains("{{Missing}}", result); // Should leave unmatched placeholders
    }

    [Fact]
    public void Process_ShouldHandleRealWorldCsprojTemplate()
    {
        // Arrange
        var template = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""9.0.10"" />
{{#if_sqlite}}
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Sqlite"" Version=""9.0.10"" />
{{/if_sqlite}}
{{#if_sqlserver}}
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""9.0.10"" />
{{/if_sqlserver}}
{{#if_postgres}}
    <PackageReference Include=""Npgsql.EntityFrameworkCore.PostgreSQL"" Version=""9.0.4"" />
{{/if_postgres}}
  </ItemGroup>
</Project>";

        var variables = new Dictionary<string, string>
        {
            { "DatabaseProvider", "postgres" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", result);
        Assert.DoesNotContain("EntityFrameworkCore.Sqlite", result);
        Assert.DoesNotContain("EntityFrameworkCore.SqlServer", result);
    }
}
