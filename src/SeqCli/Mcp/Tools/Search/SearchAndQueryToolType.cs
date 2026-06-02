// Copyright © Datalust and contributors.
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Seq.Api;
using Seq.Api.Client;
using Seq.Api.Model.Data;
using Seq.Api.Model.Events;
using Seq.Api.Model.Expressions;
using Seq.Syntax.Templates;
using SeqCli.Mapping;
using SeqCli.Mcp.Data;
using SeqCli.Output;
using Serilog;
using Serilog.Events;
using NativeFormatter = SeqCli.Output.NativeFormatter;

// ReSharper disable UnusedMember.Global

namespace SeqCli.Mcp.Tools.Search;

[McpServerToolType]
class SearchAndQueryToolType(McpSession session, SeqConnection connection)
{
    const string ResultIdPropertyName = "__seqcli_ResultId";
    static readonly ExpressionTemplate SearchResultFormatter = new (
        $"{{{ResultIdPropertyName}}} [{{UtcDateTime(@t)}} {{{LevelMapping.SurrogateLevelProperty}}}] {{@m}}\n{{#if @x is not null}}{{Substring(ToString(@x), 0, 512)}}...\n{{#end}}"
    );
    
    [McpServerTool(Name = "seq_search", ReadOnly = true, Title = "Search Events")]
    [Description("Search Seq for log events and spans matching given criteria. Each result is prefixed with " +
                 "a `result_id` of the form `R#####` which is valid in the current MCP session. Individual events can be " +
                 "viewed in full using the `seq_read_search_result` tool. Use the `seq-search-and-query` " +
                 "skill when calling this tool.")]
    [return: Description("Search results and status information.")]
    public async Task<CallToolResult> SearchEventsAsync(
        [Description("The maximum number of events to return.")]
        [Range(1, 1000)]
        int limit,
        [Description("A Seq search expression evaluated over event properties.")]
        string? predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(predicate))
        {
            if (!predicate.Contains("@Timestamp", StringComparison.OrdinalIgnoreCase) &&
                !predicate.Contains("@Id", StringComparison.OrdinalIgnoreCase) &&
                !predicate.Contains("@TraceId", StringComparison.OrdinalIgnoreCase))
            {
                return SimpleTextResult("The predicate doesn't adequately constrain the search range (by `@Timestamp`, `@TraceId`, or `@Id`). " +
                                   "To avoid consuming excessive resources, add a time bound such as `@Timestamp >= now() - 1d`.", isError: true);
            }
            
            ExpressionPart strict;
            try
            {
                strict = await connection.Expressions.ToStrictAsync(predicate, cancellationToken);
            }
            catch (Exception ex)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content =
                    [
                        new TextContentBlock
                        {
                            Text = "The Seq API client failed while attempting to validate the search expression."
                        },
                        new TextContentBlock
                        {
                            Text = ex.ToString()
                        }
                    ],
                };
            }
            if (strict.MatchedAsText)
            {
                return SimpleTextResult($"The search expression was rejected by the Seq server. {strict.ReasonIfMatchedAsText}",
                    isError: true);
            }
        }

        var resultsLock = new Lock();
        string? error = null;
        var results = new List<EventEntity>();
        var timeout = Task.Delay(TimeSpan.FromSeconds(45), cancellationToken);
        using var cancelEnumerate = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var cancelEnumerateToken = cancelEnumerate.Token;
        var enumerate = Task.Run(async () =>
        {
            try
            {
                await foreach (var evt in connection.Events.EnumerateAsync(
                                   filter: predicate,
                                   count: limit,
                                   render: true,
                                   cancellationToken: cancelEnumerateToken))
                {
                    lock (resultsLock)
                    {
                        results.Add(evt);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is not OperationCanceledException)
                {
                    Log.Error(ex, "Exception thrown during search result enumeration");
                }

                lock (resultsLock)
                {
                    error = ex.GetBaseException() is SeqApiException ? ex.GetBaseException().Message : ex.ToString();
                }
            }

        }, cancellationToken);

        var completed = await Task.WhenAny(enumerate, timeout) == enumerate;
        await cancelEnumerate.CancelAsync();

        EventEntity[] takenResults;
        string? takenError;
        lock (resultsLock)
        {
            takenResults = results.ToArray();
            takenError = error;
        }

        string resultSetStatus;
        
        var reachedLimit = takenResults.Length == limit;
        if (reachedLimit)
        {
            resultSetStatus = $"Showing the most recent {limit} matching event(s):";
        }
        else if (takenError != null)
        {
            if (takenResults.Length == 0)
            {
                resultSetStatus = $"The search failed. {takenError}";
            }
            else
            {
                resultSetStatus = $"The search failed after retrieving {takenResults.Length} matching event(s). {takenError}";
            }
        }
        else if (completed)
        {
            if (takenResults.Length == 0)
            {
                resultSetStatus = "No events matched the search expression.";
            }
            else
            {
                resultSetStatus = $"Showing all {takenResults.Length} matching event(s).";
            }
        }
        else
        {
            if (takenResults.Length == 0)
            {
                // FUTURE: point to indexes when it's possible to retrieve index info.
                resultSetStatus = "The search timed out before any results were identified. " +
                                  "Retry using narrower time ranges.";
            }
            else
            {
                resultSetStatus = $"The search timed out after retrieving {takenResults.Length} matching " +
                                  "event(s). Inspect these results, and if more are required, retry using " +
                                  "narrower time ranges.";
            }
        }
        
        var responseText = new StringWriter();
        foreach (var result in takenResults)
        {
            var resultId = session.ImportSearchResult(result);
            var serilogEvent = OutputFormat.ToSerilogEvent(result);
            serilogEvent.AddOrUpdateProperty(new LogEventProperty(ResultIdPropertyName, new ScalarValue(resultId)));
            serilogEvent.AddOrUpdateProperty(new LogEventProperty(LevelMapping.SurrogateLevelProperty, new ScalarValue(result.Level ?? "Information")));
            SearchResultFormatter.Format(serilogEvent, responseText);
        }

        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock { Text = resultSetStatus },
                new TextContentBlock { Text = responseText.ToString() }
            ]
        };
    }
    
    
    [McpServerTool(Name = "seq_read_search_result", ReadOnly = true, Title = "Read Full Event Details")]
    [Description("Read the full details of an event appearing in `seq_search` results, including all property " +
                 "values and a complete stack trace (if present). The event is formatted precisely as a Seq syntax literal, " +
                 "using Seq's native data model.")]
    [return: Description("A Seq-native object literal representation of the event data.")]
    public Task<CallToolResult> ReadSearchResultJsonAsync(
        [Description("The result id from the `seq_search` tool.")]
        // ReSharper disable once InconsistentNaming
        string result_id)
    {
        if (!session.TryGetSearchResult(result_id, out var result, out var error))
        {
            return Task.FromResult(SimpleTextResult(error, isError: true));
        }

        var resultText = new StringWriter();
        NativeFormatter.WriteEvent(resultText, result);

        return Task.FromResult(SimpleTextResult(resultText.ToString()));
    }

    [McpServerTool(Name = "seq_inspect_result_schema", ReadOnly = true, Title = "Inspect Search Result Schema")]
    [Description("List the user-defined top-level, scope, and resource property names observed on events " +
                 "in search results so far in this session. Only events retrieved in search results are considered. " +
                 "Critically important for task accuracy.")]
    [return: Description("A list containing Seq syntax-formatted property names.")]
    public Task<string[]> InspectSchemaAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(session.EnumerateUserPropertyNames(cancellationToken).OrderBy(n => n).ToArray());
    }

    [McpServerTool(Name = "seq_query", ReadOnly = true, Title = "Evaluate a Query over Logs, Spans, or Metric Samples")]
    [Description("Evaluate a Seq query, producing tabular results. Use the `seq-search-and-query` " +
                 "skill when calling this tool.")]
    [return: Description("Query results and status information.")]
    public async Task<CallToolResult> QueryAsync(
        [Description("A Seq query language query.")]
        string query,
        CancellationToken cancellationToken)
    {
        if (query.Contains("from", StringComparison.OrdinalIgnoreCase) &&
            (!query.Contains("where", StringComparison.OrdinalIgnoreCase) ||
            !query.Contains("@Timestamp", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("@Id", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("@TraceId", StringComparison.OrdinalIgnoreCase)))
        {
            return SimpleTextResult("The query doesn't adequately constrain the search range (by `@Timestamp`, `@TraceId`, or `@Id`). " +
                                    "To avoid consuming excessive resources, add a time bound such as `where @Timestamp >= now() - 1d`.", isError: true);
        }

        QueryResultPart result;
        try
        {
            result = await DataResourceGroupHelper.QueryPreserveErrorResponsesAsync(connection, query, cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex.GetBaseException() is not OperationCanceledException)
            {
                Log.Error(ex, "Exception thrown during query execution");
            }
            
            var error = ex.GetBaseException() is SeqApiException ? ex.GetBaseException().Message : ex.ToString();
            return SimpleTextResult($"The search failed. {error}", isError: true);
        }

        if (result.Error != null)
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = $"The query could not be executed. {result.Error}"
                    },
                    new TextContentBlock
                    {
                        Text = string.Join(" ", result.Reasons)
                    },
                    new TextContentBlock
                    {
                        Text = result.Suggestion != null ? $"Did you mean: {result.Suggestion}?" : ""
                    }
                ]
            };
        }

        var output = new StringWriter();
        NativeFormatter.WriteQueryResult(output, result);
        return SimpleTextResult(output.ToString());
    }
    
    [McpServerTool(Name = "seq_new_session", ReadOnly = true, Title = "Begin a new Search/Query Session")]
    [Description("Call this before interacting with Seq tools for the first time (optimizes resource usage by clearing caches).")]
    public Task NewSessionAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        session.Clear();
        return Task.CompletedTask;
    }
    
    static CallToolResult SimpleTextResult(string resultText, bool isError = false)
    {
        return new CallToolResult
        {
            IsError = isError,
            Content =
            [
                new TextContentBlock
                {
                    Text = resultText
                }
            ]
        };
    }
}