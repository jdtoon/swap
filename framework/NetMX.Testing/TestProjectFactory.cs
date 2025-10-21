using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace NetMX.Testing;

/// <summary>
/// Factory for creating temporary test projects with SQLite databases.
/// Enables isolated testing of features without requiring PostgreSQL.
/// </summary>
public static class TestProjectFactory
{
    /// <summary>
    /// Creates a temporary project directory with minimal structure for testing.
    /// </summary>
    /// <param name="projectName">Name of the test project (default: random GUID)</param>
    /// <returns>Absolute path to the created project directory</returns>
    public static string CreateTestProject(string? projectName = null)
    {
        projectName ??= $"TestProject_{Guid.NewGuid():N}";
        
        var tempDir = Path.Combine(Path.GetTempPath(), "netmx-tests", projectName);
        
        // Create directory structure
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "Models"));
        Directory.CreateDirectory(Path.Combine(tempDir, "Data"));
        Directory.CreateDirectory(Path.Combine(tempDir, "Migrations"));
        
        // Create minimal .csproj file
        var csprojContent = GenerateMinimalCsProj(projectName);
        File.WriteAllText(Path.Combine(tempDir, $"{projectName}.csproj"), csprojContent);
        
        // Create minimal DbContext
        var dbContextContent = GenerateMinimalDbContext(projectName);
        File.WriteAllText(Path.Combine(tempDir, "Data", $"{projectName}DbContext.cs"), dbContextContent);
        
        // Create SQLite connection string in appsettings.json
        var appSettingsContent = GenerateAppSettings(projectName);
        File.WriteAllText(Path.Combine(tempDir, "appsettings.json"), appSettingsContent);
        
        return tempDir;
    }

    /// <summary>
    /// Creates an in-memory SQLite connection for testing.
    /// Connection must be kept open for the duration of the test.
    /// </summary>
    /// <returns>Open SQLite connection</returns>
    public static SqliteConnection CreateInMemoryConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Creates DbContextOptions configured for in-memory SQLite testing.
    /// </summary>
    /// <typeparam name="TContext">DbContext type</typeparam>
    /// <param name="connection">Open SQLite connection</param>
    /// <returns>DbContextOptions for the context</returns>
    public static DbContextOptions<TContext> CreateSqliteOptions<TContext>(SqliteConnection connection)
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseSqlite(connection)
            .Options;
    }

    /// <summary>
    /// Cleans up a test project directory.
    /// </summary>
    /// <param name="projectPath">Path to the project directory</param>
    public static void CleanupTestProject(string projectPath)
    {
        if (Directory.Exists(projectPath))
        {
            try
            {
                Directory.Delete(projectPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors (files may be locked)
            }
        }
    }

    /// <summary>
    /// Generates a minimal .csproj file for testing.
    /// </summary>
    private static string GenerateMinimalCsProj(string projectName)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Sqlite"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""9.0.0"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>";
    }

    /// <summary>
    /// Generates a minimal DbContext for testing.
    /// </summary>
    private static string GenerateMinimalDbContext(string projectName)
    {
        return $@"using Microsoft.EntityFrameworkCore;

namespace {projectName}.Data;

public class {projectName}DbContext : DbContext
{{
    public {projectName}DbContext(DbContextOptions<{projectName}DbContext> options)
        : base(options)
    {{
    }}

    // DbSets will be added here by CLI
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {{
        base.OnModelCreating(modelBuilder);
        
        // Entity configurations will be added here
    }}
}}";
    }

    /// <summary>
    /// Generates appsettings.json with SQLite connection string.
    /// </summary>
    private static string GenerateAppSettings(string projectName)
    {
        var dbPath = Path.Combine("Data", $"{projectName}.db");
        return $@"{{
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""Data Source={dbPath}""
  }},
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.EntityFrameworkCore"": ""Warning""
    }}
  }}
}}";
    }
}
