using System;
using Seq.Api.Model;

#nullable enable

namespace SeqCli.Templates.Export
{
    static class TemplateResource
    {
        public static string FromEntityType(Type entityType)
        {
            if (!typeof(Entity).IsAssignableFrom(entityType))
                throw new ArgumentException("Type is not an entity type.");
            
            return entityType.Name.ToLowerInvariant().Replace("entity", "");
        }

        public static string ToResourceGroup(string resource)
        {
            if (!resource.EndsWith("y"))
                return resource + "s";

            return resource.TrimEnd('y') + "ies";
        }
    }
}
