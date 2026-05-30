using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Seq.Api.Model.Events;
using SeqCli.Mcp.Formatting;

namespace SeqCli.Mcp.Schema;

static class EventEntitySchema
{
    const int MaxAccessorPathDepth = 5;
    
    public static IEnumerable<string> EnumeratePropertyAccessorPaths(EventEntity evt)
    {
        foreach (var property in evt.Properties ?? [])
        {
            foreach (var accessor in EnumerateAccessorPaths("@Properties", true, property.Name, property.Value, 1))
                yield return accessor;
        }
        
        foreach (var property in evt.Scope ?? [])
        {
            foreach (var accessor in EnumerateAccessorPaths("@Scope", false, property.Name, property.Value, 1))
                yield return accessor;
        }
        
        foreach (var property in evt.Resource ?? [])
        {
            foreach (var accessor in EnumerateAccessorPaths("@Resource", false, property.Name, property.Value, 1))
                yield return accessor;
        }
    }

    static IEnumerable<string> EnumerateAccessorPaths(string prefixPath, bool optionalPrefix, string propertyName, object? propertyValue, int depth)
    {
        var name = SeqSyntaxFormatter.MakeIdentifier(prefixPath, propertyName, optionalPrefix);
        yield return name;
        
        if (depth < MaxAccessorPathDepth && propertyValue is JObject jo)
        {
            foreach (var child in jo.Properties())
            {
                foreach (var childName in EnumerateAccessorPaths(name, false, child.Name, child.Value, depth + 1))
                    yield return childName;
            }
        }
    }
}