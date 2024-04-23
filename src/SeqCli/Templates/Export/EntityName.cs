using System;
using Seq.Api.Model;

namespace SeqCli.Templates.Export;

static class EntityName
{
    public static string FromEntityType(Type entityType)
    {
        if (!typeof(Entity).IsAssignableFrom(entityType))
            throw new ArgumentException("Type is not an entity type.");
            
        return entityType.Name.ToLowerInvariant().Replace("entity", "");
    }

    public static string ToResourceGroup(string resource)
    {
        if (resource.Equals("expressionindex"))
        {
            return "expressionindexes";
        }
        
        if (!resource.EndsWith("y"))
            return resource + "s";

        return resource.TrimEnd('y') + "ies";
    }
}