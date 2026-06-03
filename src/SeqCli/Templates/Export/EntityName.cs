using System;
using Seq.Api.Model;
using Seq.Api.Model.Queries;

namespace SeqCli.Templates.Export;

static class EntityName
{
    public static string FromEntityType(Type entityType)
    {
        if (!typeof(Entity).IsAssignableFrom(entityType))
            throw new ArgumentException("Type is not an entity type.");

        if (typeof(QueryEntity) == entityType)
            return "sqlquery";
            
        return entityType.Name.ToLowerInvariant().Replace("entity", "");
    }

    public static string ToResourceGroup(string resource)
    {
        if (resource == "query")
            return "sqlqueries";
        
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