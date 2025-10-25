using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;
using carestream.core.infrastructure; // Added for ICurrentFacilityContext

namespace carestream.persistence.repositories
{
    /// <summary>
    /// Provides common base functionality for Dapper repositories,
    /// including connection string management and a helper for executing database operations
    /// with managed connection lifetimes.
    /// </summary>
    public abstract class BaseRepository
    {
        private readonly string _connectionString;
        protected readonly ILogger _logger;
        protected readonly ICurrentFacilityContext _facilityContext; // ADDED: Field for facility context

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration for retrieving connection strings.</param>
        /// <param name="logger">The logger instance for this repository.</param>
        /// <param name="facilityContext">The current facility context.</param>
        /// <exception cref="ArgumentNullException">Thrown if configuration, logger, or facilityContext is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the DefaultConnection string is not found or is empty.</exception>
        protected BaseRepository(IConfiguration configuration, ILogger logger, ICurrentFacilityContext facilityContext) // MODIFIED: Added facilityContext parameter
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ArgumentNullException.ThrowIfNull(configuration);
            _facilityContext = facilityContext ?? throw new ArgumentNullException(nameof(facilityContext)); // ADDED: Assign facility context

            _connectionString = configuration.GetConnectionString("DefaultConnection")!;

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                var host = configuration["POSTGRES_HOST"] ?? "postgres";
                var port = configuration["POSTGRES_PORT"] ?? "5432";
                var db = configuration["POSTGRES_DB"];
                var user = configuration["POSTGRES_USER"];
                var pass = configuration["POSTGRES_PASSWORD"];

                if (string.IsNullOrEmpty(db) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                {
                    _logger.LogError("Database connection details incomplete. 'DefaultConnection' not found and fallback POSTGRES_HOST/DB/USER/PASSWORD environment variables are also incomplete.");
                    throw new InvalidOperationException("Database connection details incomplete. Ensure 'DefaultConnection' or all POSTGRES_ environment variables are set.");
                }
                _connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass};";
                _logger.LogInformation("[REPO_BASE] Using constructed connection string for {RepositoryType}.", GetType().Name);
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                _logger.LogError("Database connection string resolved to null or empty for {RepositoryType}.", GetType().Name);
                throw new InvalidOperationException("Database connection string cannot be null or empty.");
            }
            _logger.LogInformation("[REPO_BASE] {RepositoryType} initialized with connection string.", GetType().Name);
        }

        /// <summary>
        /// Creates a new NpgsqlConnection instance using the configured connection string.
        /// </summary>
        /// <returns>A new <see cref="NpgsqlConnection"/>.</returns>
        protected NpgsqlConnection CreateOwnConnection() => new NpgsqlConnection(_connectionString);

        /// <summary>
        /// Helper to determine if the provided connection (typically from an outer transaction scope like in tests)
        /// should be disposed by the current execution method.
        /// </summary>
        /// <param name="providedConnection">The externally provided database connection, if any.</param>
        /// <returns>True if the connection was not provided (i.e., it will be created by <see cref="CreateOwnConnection"/> and needs disposal); false otherwise.</returns>
        protected bool ShouldDisposeConnection(IDbConnection? providedConnection) => providedConnection == null;

        /// <summary>
        /// Executes a database operation that returns a result, managing connection lifetime appropriately.
        /// If a connection and transaction are provided (e.g., from integration tests), they are used.
        /// Otherwise, a new connection is created, opened, used, closed, and disposed.
        /// </summary>
        /// <typeparam name="T">The type of the result expected from the database operation.</typeparam>
        /// <param name="databaseOperation">The asynchronous database operation to execute. It receives the <see cref="IDbConnection"/> and optional <see cref="IDbTransaction"/>.</param>
        /// <param name="connection">An optional existing database connection. If null, a new one is created.</param>
        /// <param name="transaction">An optional existing database transaction. Used if provided with a connection.</param>
        /// <returns>The result of the database operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databaseOperation"/> is null.</exception>
        protected async Task<T> ExecuteWithConnectionAsync<T>(
            Func<IDbConnection, IDbTransaction?, Task<T>> databaseOperation,
            IDbConnection? connection,
            IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(databaseOperation);

            NpgsqlConnection? internalConnection = null;
            bool disposeConnection = ShouldDisposeConnection(connection);
            IDbConnection connToUse;

            if (disposeConnection)
            {
                internalConnection = CreateOwnConnection();
                connToUse = internalConnection;
            }
            else if (connection is NpgsqlConnection npgsqlProvidedConnection)
            {
                connToUse = npgsqlProvidedConnection;
            }
            else
            {
                _logger.LogWarning("ExecuteWithConnectionAsync received a non-NpgsqlConnection ({ProvidedType}). This may lead to unexpected behavior with async operations if the provided connection does not support them correctly.", connection?.GetType().FullName);
                connToUse = connection!;
            }

            try
            {
                if (disposeConnection && connToUse.State != ConnectionState.Open)
                {
                    await ((NpgsqlConnection)connToUse).OpenAsync();
                }
                return await databaseOperation(connToUse, transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during database operation in ExecuteWithConnectionAsync for {RepositoryType}.", GetType().Name);
                throw;
            }
            finally
            {
                if (disposeConnection && internalConnection?.State == ConnectionState.Open)
                {
                    await internalConnection.CloseAsync();
                    await internalConnection.DisposeAsync();
                }
            }
        }
    }
}