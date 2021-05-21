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
using System.Net;
using System.Threading.Tasks;
using Seq.Api;
using Seq.Api.Model;
using Seq.Api.Model.Root;
using SeqCli.Templates.Ast;
using SeqCli.Templates.Evaluator;
using SeqCli.Templates.ObjectGraphs;
using Serilog;

// ReSharper disable SuggestBaseTypeForParameter

namespace SeqCli.Templates.Import
{
    static class TemplateSetImporter
    {
        public static async Task<string> ImportAsync(
            IEnumerable<EntityTemplate> templates,
            SeqConnection connection,
            IReadOnlyDictionary<string, JsonTemplate> args,
            TemplateImportState state)
        {
            var ordering = new[] {"users", "signals", "apps", "appinstances",
                "dashboards", "sqlqueries", "workspaces", "retentionpolicies"}.ToList();

            var sorted = templates.OrderBy(t => ordering.IndexOf(t.ResourceGroup));
            
            var apiRoot = await connection.Client.GetRootAsync();
            
            foreach (var entityTemplateFile in sorted)
            {
                var err = await ApplyTemplateAsync(entityTemplateFile, args, state, connection, apiRoot);
                if (err != null)
                    return err;
            }

            return null;
        }

        static async Task<string> ApplyTemplateAsync(
            EntityTemplate template,
            IReadOnlyDictionary<string, JsonTemplate> templateArgs, 
            TemplateImportState state,
            SeqConnection connection,
            RootEntity apiRoot)
        {
            bool Ref(JsonTemplate[] args, out JsonTemplate result, out string err)
            {
                if (args.Length != 1 || args[0] is not JsonTemplateString { Value: { } filename })
                {
                    result = null;
                    err = "The `ref()` function accepts a single string argument corresponding to the referenced template filename.";
                    return false;
                }

                if (!state.TryGetCreatedEntityId(filename, out var referencedId))
                {
                    result = null;
                    err = $"The referenced template file `{filename}` does not exist or has not been evaluated.";
                    return false;
                }

                result = new JsonTemplateString(referencedId);
                err = null;
                return true;
            }

            bool Arg(JsonTemplate[] args, out JsonTemplate result, out string err)
            {
                if (args.Length != 1 || args[0] is not JsonTemplateString { Value: { } templateArgName })
                {
                    result = null;
                    err = "The `arg()` function accepts a single string argument corresponding to the template argument name.";
                    return false;
                }

                if (!templateArgs.TryGetValue(templateArgName, out result) ||
                    result == null)
                {
                    err = $"The argument `{templateArgName}` is not defined.";
                    return false;
                }

                err = null;
                return true;
            }

            var functions = new Dictionary<string, JsonTemplateFunction>
            {
                ["ref"] = Ref,
                ["arg"] = Arg
            };
            
            if (!JsonTemplateEvaluator.TryEvaluate(template.Entity, functions, out var entity, out var error))
                return error;

            var asObject = JsonTemplateObjectGraphConverter.Convert(entity);

            var resourceGroupLink = template.ResourceGroup + "Resources";
            var link = apiRoot.Links.Single(l => resourceGroupLink.Equals(l.Key, StringComparison.OrdinalIgnoreCase));
            var resourceGroup = await connection.Client.GetAsync<ResourceGroup>(apiRoot, link.Key);

            if (state.TryGetCreatedEntityId(template.Name, out var existingId) &&
                await CheckEntityExistenceAsync(connection, resourceGroup, existingId))
            {
                ((IDictionary<string, object>) asObject)["Id"] = existingId;
                await UpdateEntityAsync(connection, resourceGroup, asObject, existingId);
                Log.Information("Updated existing entity {EntityId} from {TemplateName}", existingId, template.Name);
            }
            else
            {
                var createdId = await CreateEntityAsync(connection, resourceGroup, asObject);
                state.AddOrUpdateCreatedEntityId(template.Name, createdId);
                Log.Information("Created new entity {EntityId} from {TemplateName}", createdId, template.Name);
            }
            
            return null;
        }

        static async Task<string> CreateEntityAsync(SeqConnection connection, ResourceGroup resourceGroup, object entity)
        {
            var response = await connection.Client.PostAsync<object, GenericEntity>(resourceGroup, "Items", entity);
            return response.Id;
        }

        static async Task<bool> CheckEntityExistenceAsync(SeqConnection connection, ResourceGroup resourceGroup, string id)
        {
            var link = resourceGroup.Links["Item"].GetUri(new Dictionary<string, object>
            {
                ["id"] = id
            });
            var responseMessage = await connection.Client.HttpClient.GetAsync(link);
            return responseMessage.StatusCode == HttpStatusCode.OK;
        }

        static async Task UpdateEntityAsync(SeqConnection connection, ResourceGroup resourceGroup, object entity, string id)
        {
            await connection.Client.PutAsync(resourceGroup, "Item", entity, new Dictionary<string, object>
            {
                ["id"] = id
            });            
        }
    }
}
