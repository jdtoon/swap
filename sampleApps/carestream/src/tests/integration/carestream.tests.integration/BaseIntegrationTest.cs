using Xunit.Abstractions;
using Npgsql;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using carestream.core.infrastructure;

namespace carestream.tests.integration
{
    /// <summary>
    /// Base class for integration tests. Manages a database connection and
    /// provides per-test transaction capabilities.
    /// The DatabaseTestFixture provides the schema-migrated database instance.
    /// </summary>
    public abstract class BaseIntegrationTest : IClassFixture<DatabaseTestFixture>, IDisposable // Removed IAsyncLifetime from here
    {
        protected readonly DatabaseTestFixture Fixture;

        /// <summary>
        /// Gets the active, open database connection for the current test method.
        /// </summary>
        protected NpgsqlConnection Connection { get; } // Connection opened in constructor

        /// <summary>
        /// Gets the active database transaction for the current test method.
        /// </summary>
        protected IDbTransaction Transaction { get; } // Transaction started in constructor

        /// <summary>
        /// Gets the application configuration, pre-configured with the test database connection string.
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseIntegrationTest"/> class.
        /// This constructor is called by xUnit before each test method.
        /// It opens a database connection and starts a transaction.
        /// </summary>
        /// <param name="fixture">The database test fixture, injected by xUnit.</param>
        /// <param name="output">The xUnit test output helper, injected by xUnit.</param>
        protected BaseIntegrationTest(DatabaseTestFixture fixture, ITestOutputHelper output)
        {
            Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            Fixture.Output = output; // Pass the output helper to the fixture

            var configData = new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", Fixture.ConnectionString }
            };
            Configuration = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();

            // Open connection and begin transaction for each test method
            // xUnit creates a new instance of this test class for each test method.
            Connection = new NpgsqlConnection(Fixture.ConnectionString);
            try
            {
                Connection.Open(); // Synchronous open, or use Connection.OpenAsync().GetAwaiter().GetResult() if truly needed async here
                Fixture.Output?.WriteLine($"[BaseTest CONSTRUCTOR] Connection opened for test in {this.GetType().Name}. State: {Connection.State}");
            }
            catch (Exception ex)
            {
                Fixture.Output?.WriteLine($"[BaseTest CONSTRUCTOR] FAILED to open connection: {ex.Message}");
                throw; // Fail fast if connection can't be opened
            }

            Transaction = Connection.BeginTransaction();
            Fixture.Output?.WriteLine($"[BaseTest CONSTRUCTOR] Transaction BEGIN for test in {this.GetType().Name}");
        }

        /// <summary>
        /// Disposes of resources after each test method execution.
        /// This method is called by xUnit due to implementing IDisposable.
        /// It rolls back the transaction and closes/disposes the connection.
        /// </summary>
        public void Dispose()
        {
            Fixture.Output?.WriteLine($"[BaseTest DISPOSE] Starting dispose for test in {this.GetType().Name}");
            try
            {
                Transaction?.Rollback();
                Fixture.Output?.WriteLine($"[BaseTest DISPOSE] Transaction ROLLBACK successful.");
            }
            catch (Exception ex)
            {
                Fixture.Output?.WriteLine($"[BaseTest DISPOSE ERROR] Error during transaction rollback: {ex.Message}");
            }
            finally
            {
                Transaction?.Dispose();
            }

            try
            {
                if (Connection != null && Connection.State == ConnectionState.Open)
                {
                    Connection.Close();
                    Fixture.Output?.WriteLine($"[BaseTest DISPOSE] Connection closed.");
                }
            }
            catch (Exception ex)
            {
                Fixture.Output?.WriteLine($"[BaseTest DISPOSE ERROR] Error during connection close: {ex.Message}");
            }
            finally
            {
                Connection?.Dispose();
            }
            GC.SuppressFinalize(this);
            Fixture.Output?.WriteLine($"[BaseTest DISPOSE] Dispose completed for test in {this.GetType().Name}");
        }

        /// <summary>
        /// Provides a simple mock logger instance for derived test classes.
        /// </summary>
        protected ILogger<T> GetMockLogger<T>() where T : class
        {
            return new Mock<ILogger<T>>().Object;
        }

        /// <summary>
        /// Provides a simple mock facility context provider
        /// </summary>
        protected ICurrentFacilityContext GetCurrentFacilityContext()
        {
            return new Mock<ICurrentFacilityContext>().Object;
        }
    }
}