using Swap.CLI.Infrastructure;
using Xunit;

namespace Swap.CLI.Tests.Infrastructure;

public class SeedHelperTests
{
    [Fact]
    public void ParseModelProperties_ExcludesIdProperty()
    {
        var modelContent = @"
public class Post
{
    public int Id { get; set; }
    public string Title { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);

        Assert.DoesNotContain(fields, f => f.Name == "Id");
        Assert.Contains(fields, f => f.Name == "Title");
    }

    [Fact]
    public void GenerateFakerRules_ExcludesPatternProperties_Auditable()
    {
        var modelContent = @"
public class Post : IAuditable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);
        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules("TestApp", "Post", fields);

        // Should include Title
        Assert.Contains("Title", rules);
        
        // Should exclude IAuditable properties
        Assert.DoesNotContain("CreatedAt", rules);
        Assert.DoesNotContain("CreatedBy", rules);
        Assert.DoesNotContain("UpdatedAt", rules);
        Assert.DoesNotContain("UpdatedBy", rules);
    }

    [Fact]
    public void GenerateFakerRules_ExcludesPatternProperties_SoftDeletable()
    {
        var modelContent = @"
public class Post : ISoftDeletable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);
        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules("TestApp", "Post", fields);

        // Should include Title
        Assert.Contains("Title", rules);
        
        // Should exclude ISoftDeletable properties
        Assert.DoesNotContain("IsDeleted", rules);
        Assert.DoesNotContain("DeletedAt", rules);
        Assert.DoesNotContain("DeletedBy", rules);
    }

    [Fact]
    public void GenerateFakerRules_ExcludesPatternProperties_Versionable()
    {
        var modelContent = @"
public class Post : IVersionable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int Version { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);
        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules("TestApp", "Post", fields);

        // Should include Title
        Assert.Contains("Title", rules);
        
        // Should exclude IVersionable properties
        Assert.DoesNotContain("Version", rules);
    }

    [Fact]
    public void GenerateFakerRules_ExcludesPatternProperties_Visibility()
    {
        var modelContent = @"
public class Post : IVisibility
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool IsVisible { get; set; }
    public DateTime? VisibleFrom { get; set; }
    public DateTime? VisibleUntil { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);
        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules("TestApp", "Post", fields);

        // Should include Title
        Assert.Contains("Title", rules);
        
        // Should exclude IVisibility properties
        Assert.DoesNotContain("IsVisible", rules);
        Assert.DoesNotContain("VisibleFrom", rules);
        Assert.DoesNotContain("VisibleUntil", rules);
    }

    [Fact]
    public void GenerateFakerRules_ExcludesPatternProperties_Orderable()
    {
        var modelContent = @"
public class Post : IOrderable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int Position { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);
        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules("TestApp", "Post", fields);

        // Should include Title
        Assert.Contains("Title", rules);
        
        // Should exclude IOrderable properties
        Assert.DoesNotContain("Position", rules);
    }

    [Fact]
    public void GenerateFakerRules_IncludesPublishedAt_NotPatternProperty()
    {
        var modelContent = @"
public class Post
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime PublishedAt { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);
        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules("TestApp", "Post", fields);

        // Should include both Title and PublishedAt
        Assert.Contains("Title", rules);
        Assert.Contains("PublishedAt", rules);
    }

    [Fact]
    public void GenerateFakerRules_SlugHasUniqueSuffix()
    {
        var modelContent = @"
public class Post : ISluggable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);
        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules("TestApp", "Post", fields);

        // Slug should have random suffix for uniqueness
        Assert.Contains("Slug", rules);
        Assert.Contains("AlphaNumeric(6)", rules);
    }

    [Fact]
    public void GenerateFakerRules_HandlesForeignKeys()
    {
        var modelContent = @"
public class Post
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int AuthorId { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);
        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules("TestApp", "Post", fields);

        // Prelude should load author IDs
        Assert.Contains("authorIds", prelude);
        Assert.Contains("Authors", prelude);
        
        // Rules should pick random from authorIds
        Assert.Contains("AuthorId", rules);
        Assert.Contains("PickRandom(authorIds)", rules);
    }

    [Fact]
    public void GenerateFakerRules_HandlesMultiplePatterns()
    {
        var modelContent = @"
public class Post : ISoftDeletable, IAuditable, IVersionable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public int Version { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);
        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules("TestApp", "Post", fields);

        // Should include application properties
        Assert.Contains("Title", rules);
        Assert.Contains("Body", rules);
        
        // Should exclude all pattern properties
        Assert.DoesNotContain("CreatedAt", rules);
        Assert.DoesNotContain("CreatedBy", rules);
        Assert.DoesNotContain("UpdatedAt", rules);
        Assert.DoesNotContain("UpdatedBy", rules);
        Assert.DoesNotContain("IsDeleted", rules);
        Assert.DoesNotContain("DeletedAt", rules);
        Assert.DoesNotContain("DeletedBy", rules);
        Assert.DoesNotContain("Version", rules);
    }

    [Fact]
    public void GenerateFakerRules_SmartStringRules()
    {
        var modelContent = @"
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }
    public string Description { get; set; }
}";

        var fields = SeedHelper.ParseModelProperties(modelContent);
        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules("TestApp", "User", fields);

        // Check smart field detection
        Assert.Contains("Email", rules);
        Assert.Contains("Internet.Email()", rules);
        Assert.Contains("Username", rules);
        Assert.Contains("Internet.UserName()", rules);
        Assert.Contains("FirstName", rules);
        Assert.Contains("Name.FirstName()", rules);
        Assert.Contains("LastName", rules);
        Assert.Contains("Name.LastName()", rules);
        Assert.Contains("Phone", rules);
        Assert.Contains("Phone.PhoneNumber()", rules);
        Assert.Contains("Description", rules);
        Assert.Contains("Lorem.Paragraph()", rules);
    }
}
