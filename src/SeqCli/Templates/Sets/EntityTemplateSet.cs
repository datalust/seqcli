// Copyright Datalust Pty Ltd and Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using Seq.Api.Model;
using Seq.Api.Model.Root;
using SeqCli.Templates.Ast;
using SeqCli.Templates.Evaluator;
using SeqCli.Templates.Files;
using SeqCli.Templates.ObjectGraphs;

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
            bool Ref(JsonTemplate[] args, out JsonTemplate result, out string err)
            {
                if (args.Length != 1 || !(args[0] is JsonTemplateString { Value: { } filename }))
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

                result = new JsonTemplateString(referencedId);
                err = null;
                return true;
            }

            var functions = new Dictionary<string, JsonTemplateFunction> {["ref"] = Ref};
            if (!JsonTemplateEvaluator.TryEvaluate(template.Entity, functions, out var entity, out var error))
                return error;

            var asObject = JsonTemplateObjectGraphConverter.Convert(entity);

            var resourceGroupLink = template.ResourceGroup + "Resources";
            var link = apiRoot.Links.Single(l => resourceGroupLink.Equals(l.Key, StringComparison.OrdinalIgnoreCase));
            var resourceGroup = await connection.Client.GetAsync<ResourceGroup>(apiRoot, link.Key);
            var response = await connection.Client.PostAsync<object, GenericEntity>(resourceGroup, "Items", asObject);
            ids.Add(template.Name, response.Id);
            return null;
        }
    }
}
