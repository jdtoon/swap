namespace Swap.CLI.Commands.Relationships.Models;

/// <summary>
/// Defines the type of relationship between two entities
/// </summary>
public enum RelationshipType
{
    /// <summary>
    /// One parent entity has many child entities (e.g., Customer has many Orders)
    /// </summary>
    OneToMany,

    /// <summary>
    /// One child entity belongs to one parent entity (e.g., Order belongs to Customer)
    /// This is the same as OneToMany but from the child's perspective
    /// </summary>
    ManyToOne,

    /// <summary>
    /// Many entities on both sides (e.g., Posts have many Tags, Tags have many Posts)
    /// Requires a junction table
    /// </summary>
    ManyToMany,

    /// <summary>
    /// One entity has exactly one related entity (e.g., User has one Profile)
    /// </summary>
    OneToOne
}
