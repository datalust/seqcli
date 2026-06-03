using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seq.Api;
using Seq.Api.Model;
using Seq.Api.Model.Alerting;
using Seq.Api.Model.Dashboarding;
using Seq.Api.Model.Indexing;
using Seq.Api.Model.Metrics;
using Seq.Api.Model.Retention;
using Seq.Api.Model.Signals;
using Seq.Api.Model.Queries;
using Seq.Api.Model.Workspaces;
using Serilog;

namespace SeqCli.Templates.Export;

class TemplateSetExporter
{
    readonly SeqConnection _connection;
    readonly HashSet<string?> _include;
    readonly string _outputDir;

    public TemplateSetExporter(SeqConnection connection, HashSet<string?> include, string outputDir)
    {
        _connection = connection;
        _include = include;
        _outputDir = outputDir;
    }
        
    public async Task ExportTemplateSet()
    {
        var templateValueMap = new TemplateValueMap();
        templateValueMap.MapNonNullAsArg<DashboardEntity>(nameof(DashboardEntity.OwnerId), "ownerId");
        templateValueMap.MapNonNullAsArg<SignalEntity>(nameof(SignalEntity.OwnerId), "ownerId");
        templateValueMap.MapNonNullAsArg<QueryEntity>(nameof(QueryEntity.OwnerId), "ownerId");
        templateValueMap.MapNonNullAsArg<WorkspaceEntity>(nameof(WorkspaceEntity.OwnerId), "ownerId");
        templateValueMap.MapNonNullAsArg<NotificationChannelPart>(nameof(NotificationChannelPart.NotificationAppInstanceId), "notificationAppInstanceId");
        templateValueMap.MapAsReference<SignalExpressionPart>(nameof(SignalExpressionPart.SignalId));
        templateValueMap.MapAsReferenceList<WorkspaceContentPart>(nameof(WorkspaceContentPart.DashboardIds));
        templateValueMap.MapAsReferenceList<WorkspaceContentPart>(nameof(WorkspaceContentPart.QueryIds));
        templateValueMap.MapAsReferenceList<WorkspaceContentPart>(nameof(WorkspaceContentPart.SignalIds));
        templateValueMap.Ignore<AlertEntity>(nameof(AlertEntity.Activity));
            
        await ExportTemplates<SignalEntity>(
            id => _connection.Signals.FindAsync(id),
            () => _connection.Signals.ListAsync(shared: true),
            signal => signal.Title,
            templateValueMap);
            
        await ExportTemplates<QueryEntity>(
            id => _connection.Queries.FindAsync(id),
            () => _connection.Queries.ListAsync(shared: true),
            query => query.Title,
            templateValueMap);
            
        await ExportTemplates<DashboardEntity>(
            id => _connection.Dashboards.FindAsync(id),
            () => _connection.Dashboards.ListAsync(shared: true),
            dashboard => dashboard.Title,
            templateValueMap);
            
        await ExportTemplates<AlertEntity>(
            id => _connection.Alerts.FindAsync(id),
            () => _connection.Alerts.ListAsync(shared: true),
            alert => alert.Title,
            templateValueMap);
            
        await ExportTemplates<WorkspaceEntity>(
            id => _connection.Workspaces.FindAsync(id),
            () => _connection.Workspaces.ListAsync(shared: true),
            workspace => workspace.Title,
            templateValueMap);
            
        await ExportTemplates<RetentionPolicyEntity>(
            id => _connection.RetentionPolicies.FindAsync(id),
            () => _connection.RetentionPolicies.ListAsync(),
            retentionPolicy => retentionPolicy.Id.Replace("retentionpolicy-", ""),
            templateValueMap);
        
        await ExportTemplates<ExpressionIndexEntity>(
            id => _connection.ExpressionIndexes.FindAsync(id),
            () => _connection.ExpressionIndexes.ListAsync(),
            expressionIndex => expressionIndex.Expression.All(char.IsLetterOrDigit) 
                ? expressionIndex.Expression 
                : expressionIndex.Id.Replace("expressionindex-", ""),
            templateValueMap);
        
        await ExportTemplates<ViewEntity>(
            id => _connection.Views.FindAsync(id),
            () => _connection.Views.ListAsync(shared: true),
            view => view.Title,
            templateValueMap);
    }

    async Task ExportTemplates<TEntity>(
        Func<string, Task<TEntity>> findEntity,
        Func<Task<List<TEntity>>> listEntities,
        Func<TEntity, string> getTitle,
        TemplateValueMap templateValueMap)
        where TEntity : Entity
    {
        List<TEntity> entities;
        if (_include.Count == 0)
        {
            entities = await listEntities();
        }
        else
        {
            var idPrefix = EntityName.FromEntityType(typeof(TEntity)) + "-";
            entities = new List<TEntity>();
            foreach (var id in _include.Where(i => i != null && i.StartsWith(idPrefix)))
            {
                entities.Add(await findEntity(id!));
            }
        }
            
        foreach (var entity in entities)
        {
            var filename = OutputFilename<TEntity>(getTitle(entity));
            templateValueMap.AddReferencedTemplate(entity.Id, Path.GetFileName(filename));
            await using var f = File.Create(filename);
            await using var w = new StreamWriter(f, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            await TemplateWriter.WriteTemplateAsync(w, entity, templateValueMap);
            Log.Information("Exported {EntityId} to {Filename}", entity.Id, filename);
        }
    }

    string OutputFilename<TEntity>(string title) where TEntity : Entity
    {
        var pathSafeTitle = new string(title.Select(c => c != ':' && c != '/' && c != '\\' ? c : '_').ToArray());
        var resourceType = EntityName.FromEntityType(typeof(TEntity));
        return Path.Combine(_outputDir, $"{resourceType}-{pathSafeTitle}.{TemplateWriter.TemplateFileExtension}");
    }
}