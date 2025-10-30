using Swap.CLI.Commands.Relationships.Models;

namespace Swap.CLI.Commands.Relationships;

/// <summary>
/// Validates relationship definitions before generation
/// </summary>
public class RelationshipValidator
{
    public ValidationResult Validate(RelationshipDefinition definition)
    {
        var errors = new List<string>();

        // Basic required fields
        if (string.IsNullOrWhiteSpace(definition.SourceEntity))
        {
            errors.Add("Source entity is required");
        }

        if (string.IsNullOrWhiteSpace(definition.TargetEntity))
        {
            errors.Add("Target entity is required");
        }

        // Prevent self-referential relationships (not supported in Phase 1)
        if (!string.IsNullOrWhiteSpace(definition.SourceEntity) && 
            !string.IsNullOrWhiteSpace(definition.TargetEntity) &&
            string.Equals(definition.SourceEntity, definition.TargetEntity, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Self-referential relationships are not currently supported");
        }

        // Validate delete behavior with requirements
        if (definition.OnDelete == DeleteBehavior.SetNull && definition.IsRequired)
        {
            errors.Add("Cannot use SetNull delete behavior with required foreign key");
        }

        // Validate many-to-many specific requirements
        if (definition.Type == RelationshipType.ManyToMany)
        {
            if (!string.IsNullOrWhiteSpace(definition.ForeignKeyName))
            {
                errors.Add("Foreign key name is not applicable for many-to-many relationships");
            }

            if (definition.IsRequired)
            {
                errors.Add("Required flag is not applicable for many-to-many relationships");
            }
        }

        // Validate one-to-one specific requirements
        if (definition.Type == RelationshipType.OneToOne)
        {
            // For now, just validate that we have the entities
            // In Phase 4, we'll add more sophisticated validation
            if (string.IsNullOrWhiteSpace(definition.SourceEntity) || 
                string.IsNullOrWhiteSpace(definition.TargetEntity))
            {
                errors.Add("One-to-one relationships require both source and target entities");
            }
        }

        // Validate junction table properties format
        if (definition.JunctionProperties != null)
        {
            if (definition.Type != RelationshipType.ManyToMany)
            {
                errors.Add("Junction properties are only valid for many-to-many relationships");
            }

            foreach (var prop in definition.JunctionProperties)
            {
                if (string.IsNullOrWhiteSpace(prop.Key))
                {
                    errors.Add("Junction property name cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(prop.Value))
                {
                    errors.Add($"Junction property type for '{prop.Key}' cannot be empty");
                }
            }
        }

        // Validate project path exists
        if (!Directory.Exists(definition.ProjectPath))
        {
            errors.Add($"Project path does not exist: {definition.ProjectPath}");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}

/// <summary>
/// Result of relationship validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
