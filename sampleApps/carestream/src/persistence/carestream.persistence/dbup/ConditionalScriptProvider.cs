// src/persistence/ConditionalScriptProvider.cs
using DbUp.Engine;
using Microsoft.Extensions.Hosting; // Requires Microsoft.Extensions.Hosting.Abstractions package
using System.Reflection;
using System.Text;
using System.Data;
using DbUp.Engine.Transactions;

namespace CareStream.Persistence.DbUp
{
    /// <summary>
    /// Provides DbUp SQL scripts conditionally based on environment and folder structure.
    /// Scripts are ordered strictly by folder prefix (01_, 02_, etc.) and then by filename within folders.
    /// Local seed scripts (99_Seed/02_Local) only run in Development environment.
    /// </summary>
    public class ConditionalScriptProvider : IScriptProvider
    {
        private readonly Assembly _assembly;
        private readonly IHostEnvironment _environment;
        private readonly string _rootNamespace;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionalScriptProvider"/> class.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded SQL scripts.</param>
        /// <param name="environment">The hosting environment (to check IsDevelopment()).</param>
        public ConditionalScriptProvider(Assembly assembly, IHostEnvironment environment)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));

            // Determine the root namespace for embedded resources, e.g., "CareStream.Persistence.Migrations"
            // Assumes 'migrations' is the top-level folder under the project root namespace
            _rootNamespace = $"{_assembly.GetName().Name}.migrations.";
        }

        public IEnumerable<SqlScript> GetScripts(IConnectionManager connectionManager)
        {
            var allScriptNames = _assembly.GetManifestResourceNames()
                                         .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                                         .ToList();

            var orderedScripts = new List<SqlScript>();

            // Define the explicit order of categories
            var categories = new List<string>
            {
                "01_SchemaOnly",
                "02_Tables",
                "03_ConstraintsAndFKs",
                "04_Indexes",
                "05_Programmability._01_Functions",
                "05_Programmability._02_Triggers",
                "05_Programmability._03_Views",
                "05_Programmability._04_StoredProcedures",
                "99_Seed._01_Main",
                "99_Seed._02_Local" // Conditional
            };

            foreach (var category in categories)
            {
                var categoryPrefix = $"{_rootNamespace}{"_"}{category.Replace("/", ".").Replace("\\", ".")}."; // Handle both / and \ in path names for consistency

                var scriptsInFolder = allScriptNames
                    .Where(name => name.StartsWith(categoryPrefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(name => name) // Sort scripts alphabetically/numerically within each folder
                    .ToList();

                foreach (var scriptName in scriptsInFolder)
                {
                    // Special handling for local seed scripts
                    if (category == "99_Seed.02_Local" && !_environment.IsDevelopment())
                    {
                        Console.WriteLine($"[DbUp] Skipping Local Seed Script (Not Development): {scriptName}");
                        continue;
                    }

                    Console.WriteLine($"[DbUp] Including Script: {scriptName} (Category: {category})");
                    orderedScripts.Add(LoadScript(scriptName));
                }
            }

            if (!orderedScripts.Any())
            {
                Console.WriteLine("[DbUp] Warning: No scripts found to execute by ConditionalScriptProvider.");
            }

            return orderedScripts;
        }

        private SqlScript LoadScript(string resourceName)
        {
            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Could not find embedded resource '{resourceName}' in assembly '{_assembly.FullName}'. Ensure Build Action is 'Embedded resource'.");
            }
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            // DbUp uses the script name for tracking in the schemaversions table.
            // Using the full resource name is fine, or you can extract just the filename.
            return new SqlScript(resourceName, content);
        }
    }
}