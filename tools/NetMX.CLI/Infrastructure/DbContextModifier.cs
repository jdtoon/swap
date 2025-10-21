namespace NetMX.CLI.Infrastructure;

/// <summary>
/// Modifies DbContext files to add DbSet properties for entities.
/// Wraps CodeModificationHelper with project-specific logic.
/// </summary>
public class DbContextModifier
{
    /// <summary>
    /// Adds a DbSet property to the DbContext in the specified project.
    /// </summary>
    /// <param name="projectDirectory">The project directory containing the DbContext</param>
    /// <param name="entityName">The entity name (e.g., "Product")</param>
    /// <param name="entityNamespace">The full namespace of the entity (optional)</param>
    /// <returns>Result indicating success or failure</returns>
    public static ModificationResult AddDbSetToContext(
        string projectDirectory,
        string entityName,
        string? entityNamespace = null)
    {
        try
        {
            // Find the DbContext file
            var dbContextFile = CodeModificationHelper.FindDbContextFile(projectDirectory);
            if (dbContextFile == null)
            {
                return ModificationResult.Failure("No DbContext file found in the project. Expected to find a file named *DbContext.cs in Data/, root, Persistence/, or Infrastructure/ folder.");
            }

            // Read the existing code
            var originalCode = File.ReadAllText(dbContextFile);

            // Validate it's valid C# before modifying
            if (!CodeModificationHelper.IsValidCSharpCode(originalCode))
            {
                return ModificationResult.Failure($"DbContext file contains syntax errors: {dbContextFile}");
            }

            // Check if entity namespace is needed
            if (string.IsNullOrEmpty(entityNamespace))
            {
                // Try to infer from project structure
                entityNamespace = InferEntityNamespace(projectDirectory, entityName);
            }

            // Add the DbSet property
            string modifiedCode;
            try
            {
                modifiedCode = CodeModificationHelper.AddDbSetProperty(originalCode, entityName, entityNamespace);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return ModificationResult.Failure($"DbSet<{entityName}> already exists in the DbContext. Skipping modification.");
            }
            catch (InvalidOperationException ex)
            {
                return ModificationResult.Failure($"Failed to modify DbContext: {ex.Message}");
            }

            // Backup original file
            var backupFile = dbContextFile + ".backup";
            File.Copy(dbContextFile, backupFile, overwrite: true);

            try
            {
                // Write modified code
                File.WriteAllText(dbContextFile, modifiedCode);

                // Validate the modified code compiles
                if (!CodeModificationHelper.IsValidCSharpCode(modifiedCode))
                {
                    // Restore backup if validation fails
                    File.Copy(backupFile, dbContextFile, overwrite: true);
                    File.Delete(backupFile);
                    return ModificationResult.Failure("Modified code contains syntax errors. Changes reverted.");
                }

                // Clean up backup on success
                File.Delete(backupFile);

                return ModificationResult.Success(
                    dbContextFile,
                    $"Added DbSet<{entityName}> to {Path.GetFileName(dbContextFile)}");
            }
            catch (Exception ex)
            {
                // Restore backup on any error
                if (File.Exists(backupFile))
                {
                    File.Copy(backupFile, dbContextFile, overwrite: true);
                    File.Delete(backupFile);
                }
                return ModificationResult.Failure($"Failed to write modified code: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return ModificationResult.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Infers the entity namespace based on project structure.
    /// </summary>
    private static string? InferEntityNamespace(string projectDirectory, string entityName)
    {
        // Look for the entity file
        var searchPatterns = new[]
        {
            $"Models/{entityName}.cs",
            $"Entities/{entityName}.cs",
            $"Domain/Entities/{entityName}.cs",
            $"Core/Entities/{entityName}.cs",
            $"{entityName}.cs"
        };

        foreach (var pattern in searchPatterns)
        {
            var files = Directory.GetFiles(projectDirectory, pattern, SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                // Extract namespace from the entity file
                var entityCode = File.ReadAllText(files[0]);
                return CodeModificationHelper.ExtractNamespace(entityCode);
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a DbSet for the entity already exists in the DbContext.
    /// </summary>
    public static bool DbSetExists(string projectDirectory, string entityName)
    {
        var dbContextFile = CodeModificationHelper.FindDbContextFile(projectDirectory);
        if (dbContextFile == null)
            return false;

        var code = File.ReadAllText(dbContextFile);
        return code.Contains($"DbSet<{entityName}>");
    }
}

/// <summary>
/// Result of a DbContext modification operation.
/// </summary>
public class ModificationResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; }
    public string? FilePath { get; private set; }

    private ModificationResult(bool isSuccess, string message, string? filePath = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        FilePath = filePath;
    }

    public static ModificationResult Success(string filePath, string message)
        => new ModificationResult(true, message, filePath);

    public static ModificationResult Failure(string message)
        => new ModificationResult(false, message);
}
