using Swap.CLI.Infrastructure;
using Xunit;

namespace Swap.CLI.Tests.Infrastructure;

public class SeederTemplateTests
{
    [Fact]
    public void EntitySeederTemplate_ShouldReplaceVariables()
    {
    var template = @"using {{ProjectName}}.Data;
using {{ProjectName}}.Models;

namespace {{ProjectName}}.Data.Seeders;

public static class {{EntityName}}Seeder
{
    public static async Task SeedAsync(AppDbContext db, IServiceProvider services, int count = {{SeedCount}}, string locale = ""{{SeedLocale}}"", bool ifEmpty = {{SeedIfEmpty}})
    {
    // Preload foreign key id lists
{{ForeignKeyPrelude}}
    var faker = new Bogus.Faker<{{ProjectName}}.Models.{{EntityName}}>(locale)
{{FakerRules}};
    }
}
";

        var vars = new Dictionary<string, string>
        {
            { "ProjectName", "MyApp" },
            { "EntityName", "Post" },
            { "SeedCount", "25" },
            { "SeedLocale", "en" },
            { "SeedIfEmpty", "true" },
            { "ForeignKeyPrelude", "        var userIds = new List<int>();" },
            { "FakerRules", "            .RuleFor(x => x.Title, f => f.Lorem.Sentence())" }
        };

        var result = TemplateEngine.Process(template, vars);
        Assert.Contains("using MyApp.Data;", result);
        Assert.Contains("class PostSeeder", result);
        Assert.Contains("int count = 25", result);
        Assert.Contains("string locale = \"en\"", result);
        Assert.Contains("bool ifEmpty = true", result);
        Assert.Contains("var userIds = new List<int>();", result);
        Assert.Contains(".RuleFor(x => x.Title", result);
    }
}
