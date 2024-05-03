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

// ReSharper disable SuggestBaseTypeForParameter, CommentTypo

namespace SeqCli.Templates.Import;

static class TemplateSetImporter
{
    public static async Task<string?> ImportAsync(
        IEnumerable<EntityTemplate> templates,
        SeqConnection connection,
        IReadOnlyDictionary<string, JsonTemplate> args,
        TemplateImportState state,
        bool merge)
    {
        var ordering = new List<string> {"users", "signals", "apps", "appinstances",
            "dashboards", "sqlqueries", "workspaces", "retentionpolicies",
            "alerts", "expressionindexes"};

        var sorted = templates.OrderBy(t => ordering.IndexOf(t.ResourceGroup));
            
        var apiRoot = await connection.Client.GetRootAsync();

        var functions = new EntityTemplateFunctions(state, args);
            
        foreach (var entityTemplateFile in sorted)
        {
            var err = await ApplyTemplateAsync(entityTemplateFile, functions, state, connection, apiRoot, merge);
            if (err != null)
                return err;
        }

        return null;
    }

    static async Task<string?> ApplyTemplateAsync(
        EntityTemplate template,
        EntityTemplateFunctions functions,
        TemplateImportState state,
        SeqConnection connection,
        RootEntity apiRoot,
        bool merge)
    {
        if (!JsonTemplateEvaluator.TryEvaluate(template.Entity, functions.Exports, out var entity, out var error))
            return error;

        var asObject = (IDictionary<string, object>) JsonTemplateObjectGraphConverter.Convert(entity)!;

        // O(Ntemplates) - easy target for optimization with some caching.
        var resourceGroupLink = template.ResourceGroup + "Resources";
        var link = apiRoot.Links.Single(l => resourceGroupLink.Equals(l.Key, StringComparison.OrdinalIgnoreCase));
        var resourceGroup = await connection.Client.GetAsync<ResourceGroup>(apiRoot, link.Key);
        
        // ExpressionIndexes with mapped ids or identical expressions are assumed to be equivalent.
        var immutableTarget = template.ResourceGroup == "ExpressionIndexes";

        if (state.TryGetCreatedEntityId(template.Name, out var existingId) &&
            await CheckEntityExistenceAsync(connection, resourceGroup, existingId))
        {
            asObject["Id"] = existingId;
            if (immutableTarget)
            {
                Log.Information("No work required for existing immutable entity {EntityId} from {TemplateName}", existingId, template.Name);
            }
            else
            {
                await UpdateEntityAsync(connection, resourceGroup, asObject, existingId);
                Log.Information("Updated existing entity {EntityId} from {TemplateName}", existingId, template.Name);
            }
        }
        else if (merge && !state.TryGetCreatedEntityId(template.Name, out _) &&
                 await TryFindMergeTargetAsync(connection, resourceGroup, asObject) is { } mergedId)
        {
            asObject["Id"] = mergedId;

            if (immutableTarget)
            {
                Log.Information("Adding merge entry for existing immutable entity {EntityId} from {TemplateName}", existingId, template.Name);
                state.AddOrUpdateCreatedEntityId(template.Name, mergedId);
            }
            else
            {
                await UpdateEntityAsync(connection, resourceGroup, asObject, mergedId);
                state.AddOrUpdateCreatedEntityId(template.Name, mergedId);
                Log.Information("Merged and updated existing entity {EntityId} from {TemplateName}", existingId, template.Name);
            }
        }
        else
        {
            var createdId = await CreateEntityAsync(connection, resourceGroup, asObject);
            state.AddOrUpdateCreatedEntityId(template.Name, createdId);
            Log.Information("Created new entity {EntityId} from {TemplateName}", createdId, template.Name);
        }
            
        return null;
    }

    static async Task<string?> TryFindMergeTargetAsync(SeqConnection connection, ResourceGroup resourceGroup, IDictionary<string, object> entity)
    {
        if (!entity.TryGetValue("Title", out var nameOrTitleValue) &&
            !entity.TryGetValue("Name", out nameOrTitleValue) &&
            !entity.TryGetValue("Expression", out nameOrTitleValue)||
            nameOrTitleValue is not string nameOrTitle)
        {
            return null;
        }

        var parameters = resourceGroup.Links["Items"].Template.Contains("shared")
            ? new Dictionary<string, object>
            {
                ["shared"] = true
            }
            : null;
            
        // O(Ntemplates*Nentities) - easy target for optimization with some caching.
        var candidates = await connection.Client.GetAsync<List<GenericEntity>>(resourceGroup, "Items", parameters);

        return candidates.FirstOrDefault(e => e.Title == nameOrTitle || e.Name == nameOrTitle || e.Expression == nameOrTitle)?.Id;
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