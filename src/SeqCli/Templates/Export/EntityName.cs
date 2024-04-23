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
        if (resource.EndsWith('y'))
        {
            return resource.TrimEnd('y') + "ies";
        }

        if (resource.EndsWith('x'))
        {
            return resource + "es";
        }

        return resource + "s";
    }
}