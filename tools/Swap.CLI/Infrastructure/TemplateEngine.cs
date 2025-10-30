namespace Swap.CLI.Infrastructure;

public class TemplateEngine
{
    public static string Process(string template, Dictionary<string, string> variables)
    {
        var result = template;
        
        // Replace variables like {{ProjectName}}
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }
        
        // Handle conditional blocks
        result = ProcessConditionals(result, variables);
        
        return result;
    }
    
    private static string ProcessConditionals(string content, Dictionary<string, string> variables)
    {
        var result = content;
        
        // Process {{#if_sqlite}}...{{/if_sqlite}}
        result = ProcessConditional(result, "if_sqlite", variables.GetValueOrDefault("DatabaseProvider") == "sqlite");
        result = ProcessConditional(result, "if_sqlserver", variables.GetValueOrDefault("DatabaseProvider") == "sqlserver");
        result = ProcessConditional(result, "if_postgres", variables.GetValueOrDefault("DatabaseProvider") == "postgres");
        
        // Process {{#if_local_nuget}}...{{/if_local_nuget}}
        result = ProcessConditional(result, "if_local_nuget", variables.GetValueOrDefault("UseLocalNuget") == "true");
        
        return result;
    }
    
    private static string ProcessConditional(string content, string condition, bool isTrue)
    {
        var startTag = $"{{{{#{condition}}}}}";
        var endTag = $"{{{{/{condition}}}}}";
        
        while (true)
        {
            var startIndex = content.IndexOf(startTag);
            if (startIndex == -1) break;
            
            var endIndex = content.IndexOf(endTag, startIndex);
            if (endIndex == -1) break;
            
            var blockContent = content.Substring(startIndex + startTag.Length, endIndex - startIndex - startTag.Length);
            var replacement = isTrue ? blockContent : "";
            
            content = content.Substring(0, startIndex) + replacement + content.Substring(endIndex + endTag.Length);
        }
        
        return content;
    }
}
