using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DbUp;
using Xunit.Abstractions;
using carestream.persistence.migrations;

namespace carestream.tests.integration
{
    /// <summary>
    /// xUnit Class Fixture for managing a shared PostgreSQL Testcontainer instance and running DbUp migrations.
    /// The database schema is set up once per test class that uses this fixture.
    /// </summary>
    public class DatabaseTestFixture : IAsyncLifetime
    {
        /// <summary>
        /// Gets the Testcontainer instance for PostgreSQL.
        /// </summary>
        public IContainer? PostgreSqlContainer { get; private set; }

        /// <summary>
        /// Gets the connection string to the test PostgreSQL database.
        /// </summary>
        public string ConnectionString { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets the xUnit test output helper for logging from the fixture.
        /// Test classes should inject ITestOutputHelper and assign it to this property in their constructor.
        /// </summary>
        public ITestOutputHelper? Output { get; set; }

        private static readonly string _dbName = $"testdb_{Guid.NewGuid():N}";
        private static readonly string _dbUser = "testuser";
        private static readonly string _dbPassword = "TestPassword123!";

        /// <summary>
        /// Initializes the PostgreSQL Testcontainer and runs DbUp schema migrations.
        /// This is called once per test class using this fixture before any tests in that class run.
        /// </summary>
        public async Task InitializeAsync()
        {
            LogFixtureMessage($"[DB FIXTURE] Initializing PostgreSQL Testcontainer (DB: {_dbName})...");

            PostgreSqlContainer = new ContainerBuilder()
                .WithImage("postgres:15")
                .WithEnvironment("POSTGRES_DB", _dbName)
                .WithEnvironment("POSTGRES_USER", _dbUser)
                .WithEnvironment("POSTGRES_PASSWORD", _dbPassword)
                .WithPortBinding(5432, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
                .Build();

            try
            {
                await PostgreSqlContainer.StartAsync();

                string host = PostgreSqlContainer.Hostname;
                ushort port = PostgreSqlContainer.GetMappedPublicPort(5432);
                ConnectionString = $"Host={host};Port={port};Database={_dbName};Username={_dbUser};Password={_dbPassword};Include Error Detail=true;";

                LogFixtureMessage($"[DB FIXTURE] Container Started. ConnectionString (masked): {ConnectionString.Replace(_dbPassword, "***")}");

                LogFixtureMessage("[DB FIXTURE] Running DbUp schema migrations...");
                EnsureDatabase.For.PostgresqlDatabase(ConnectionString);

                var persistenceAssembly = typeof(PersistenceMarker).Assembly;

                var upgrader = DeployChanges.To
                   .PostgresqlDatabase(ConnectionString)
                   .WithScriptsEmbeddedInAssembly(persistenceAssembly, scriptName =>
                   {
                       bool isSchemaScript = scriptName.Contains(".migrations.schema.");
                       LogFixtureMessage($"[DbUp Filter] Script: {scriptName,-80} | IsSchema: {isSchemaScript,-5} | WillRun: {isSchemaScript}");
                       return isSchemaScript; // Only run Schema scripts
                   }
                    )
                   .LogToConsole()
                   .LogScriptOutput()
                   .Build();

                var result = upgrader.PerformUpgrade();

                if (!result.Successful)
                {
                    LogFixtureMessage("[DB FIXTURE] SCHEMA MIGRATION FAILED!", error: true);
                    if (result.ErrorScript != null)
                    {
                        LogFixtureMessage($"[DB FIXTURE] Error in script: {result.ErrorScript.Name}", error: true);
                    }
                    LogFixtureMessage($"[DB FIXTURE] DbUp Error Details: {result.Error}", error: true);
                    throw new Exception("DbUp schema migration failed in test fixture.", result.Error);
                }
                LogFixtureMessage("[DB FIXTURE] Schema migrations successful.");
            }
            catch (Exception ex)
            {
                LogFixtureMessage($"[DB FIXTURE] CRITICAL ERROR during initialization: {ex}", error: true);
                if (PostgreSqlContainer != null && PostgreSqlContainer.State == TestcontainersStates.Running)
                {
                    await DisposeAsyncInternal(); // Attempt cleanup
                }
                throw;
            }
            LogFixtureMessage("[DB FIXTURE] Initialization Complete.");
        }

        /// <summary>
        /// Disposes of the PostgreSQL Testcontainer after all tests in the class have run.
        /// </summary>
        public async Task DisposeAsync()
        {
            await DisposeAsyncInternal();
        }

        private async Task DisposeAsyncInternal()
        {
            LogFixtureMessage("[DB FIXTURE] Disposing PostgreSQL Testcontainer...");
            if (PostgreSqlContainer != null)
            {
                try
                {
                    if (PostgreSqlContainer.State == TestcontainersStates.Running)
                    {
                        await PostgreSqlContainer.StopAsync();
                    }
                    await PostgreSqlContainer.DisposeAsync();
                }
                catch (Exception ex)
                {
                    LogFixtureMessage($"[DB FIXTURE] Error during container disposal: {ex.Message}", error: true);
                }
            }
            PostgreSqlContainer = null;
            LogFixtureMessage("[DB FIXTURE] Disposal Complete.");
        }

        /// <summary>
        /// Helper to log messages from the fixture, falling back to Console if ITestOutputHelper is not available/valid.
        /// </summary>
        private void LogFixtureMessage(string message, bool error = false)
        {
            try
            {
                Output?.WriteLine(message);
            }
            catch (InvalidOperationException) // ITestOutputHelper no longer valid
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                if (error) Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message); // Fallback to Console
                if (error) Console.ForegroundColor = originalColor;
            }
            catch (Exception ex) // Catch any other exception during logging
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[LogFixtureMessage Fallback] Error logging: '{message}'. Original logging error: {ex.Message}");
                Console.ForegroundColor = originalColor;
            }
        }
    }
}