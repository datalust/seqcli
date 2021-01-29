using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using Seq.Api.Model;
using Seq.Api.Model.Root;
using SeqCli.Templates.Evaluator;
using SeqCli.Templates.Files;

namespace SeqCli.Templates.Sets
{
    static class EntityTemplateSet
    {
        public static async Task<string> ApplyAsync(IEnumerable<EntityTemplateFile> templates, SeqConnection connection)
        {
            var ordering = new[] {"users", "signals", "apps", "appinstances",
                "dashboards", "sqlqueries", "workspaces", "retentionpolicies"}.ToList();

            var sorted = templates.OrderBy(t => ordering.IndexOf(t.ResourceGroup));
            var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            var apiRoot = await connection.Client.GetRootAsync();
            
            foreach (var entityTemplateFile in sorted)
            {
                var err = await ApplyTemplateAsync(entityTemplateFile, ids, connection, apiRoot);
                if (err != null)
                    return err;
            }

            return null;
        }

        static async Task<string> ApplyTemplateAsync(EntityTemplateFile template, IDictionary<string,string> ids, SeqConnection connection, RootEntity apiRoot)
        {
            bool Ref(object[] args, out object result, out string err)
            {
                if (args.Length != 1 || !(args[0] is string filename))
                {
                    result = null;
                    err = "The `ref()` function accepts a single string argument.";
                    return false;
                }

                if (!ids.TryGetValue(filename, out var referencedId))
                {
                    result = null;
                    err = $"The referenced template file `{filename}` does not exist or has not been evaluated.";
                    return false;
                }

                result = referencedId;
                err = null;
                return true;
            }

            var functions = new Dictionary<string, JsonTemplateFunction> {["ref"] = Ref};
            if (!JsonTemplateEvaluator.TryEvaluate(template.Entity, functions, out var entity, out var error))
                return error;

            var resourceGroupLink = template.ResourceGroup + "Resources";
            var link = apiRoot.Links.Single(l => resourceGroupLink.Equals(l.Key, StringComparison.OrdinalIgnoreCase));
            var resourceGroup = await connection.Client.GetAsync<ResourceGroup>(apiRoot, link.Key);
            var response = await connection.Client.PostAsync<object, GenericEntity>(resourceGroup, "Items", entity);
            ids.Add(template.Name, response.Id);
            return null;
        }
    }
}
