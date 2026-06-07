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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Seq.Api;
using SeqCli.Output;

// ReSharper disable UnusedMember.Global

namespace SeqCli.Mcp.Tools.Metrics;

[McpServerToolType]
class MetricsTools(McpSession session, SeqConnection connection)
{
    [McpServerTool(Name = "seq_search_metric_definitions", ReadOnly = true, Title = "Search Metric Definitions", UseStructuredContent = true)]
    [Description("Search for metric definitions matching given criteria.")]
    [return: Description("Matching metric definitions.")]
    public async Task<MetricDefinition[]> SearchMetricsAsync(
        [Description("The maximum number of metric definitions to return.")]
        [Range(1, 1000)]
        int limit,
        [Description("A Seq search expression evaluated over metric names (`Keys(@Definitions)[?]`), descriptions " +
                     "(`@Definitions[?].description`), resource attributes, scope attributes, and raw samples.")]
        string? predicate = null,
        [Description("Optionally, break down the available descriptions by grouping on one or more resource, scope, or " +
                     "sample attributes.")]
        string[]? groups = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(predicate))
        {
            if (!predicate.Contains("@Timestamp", StringComparison.OrdinalIgnoreCase))
            {
                throw new McpException(
                    "The predicate doesn't adequately constrain the search range. " +
                    "To avoid consuming excessive resources, add a time bound such as `@Timestamp >= now() - 4h`.");
            }

            var strict = await connection.Expressions.ToStrictAsync(predicate, cancellationToken);
            if (strict.MatchedAsText)
            {
                throw new McpException(
                    $"The search expression was rejected by the Seq server. {strict.ReasonIfMatchedAsText}");
            }
        }

        var definitions = await connection.Metrics.SearchAsync(groups?.ToList() ?? [], predicate, limit,
            timeout: session.DataToolCallTimeout, cancellationToken: cancellationToken);

        return definitions.Metrics.Select(m => new MetricDefinition
        {
            Name = m.Name ?? m.Accessor,
            Kind = m.Kind,
            Unit = m.Unit,
            Description = m.Description,
            GroupKeyValues = m.GroupKey
        }).ToArray();
    }

    [McpServerTool(Name = "seq_list_metric_dimensions", ReadOnly = true, Title = "List Metric Dimensions")]
    [Description("List the dimensions associated with a given metric.")]
    [return: Description("Dimension accessor expressions, one per line.")]
    public async Task<CallToolResult> ListDimensionsAsync(
        [Description("The maximum number of metric dimensions to return.")]
        [Range(1, 1000)]
        int limit,
        [Description("An ISO 8601 timestamp specifying the lower bound for the search range.")]
        DateTimeOffset from,
        [Description("The upper bound for the search range.")]
        DateTimeOffset to,
        [Description("A human-readable metric name, for example `hats-sold` or `http.request.duration`; omit to list dimensions for all metrics.")]
        string? metric = null,
        CancellationToken cancellationToken = default)
    {
        var dimensions = await connection.Metrics.ListDimensionsAsync(limit, metric, from.UtcDateTime, to.UtcDateTime,
            session.DataToolCallTimeout, cancellationToken: cancellationToken);

        var result = new StringWriter();
        foreach (var dimension in dimensions)
        {
            await result.WriteLineAsync(dimension.Accessor);
        }

        return McpResults.SimpleText(result.ToString());
    }

    [McpServerTool(Name = "seq_list_metric_dimension_values", ReadOnly = true, Title = "List Metric Dimension Values")]
    [Description("List the unique values present in a given metric dimension.")]
    [return: Description("Dimension values in Seq native syntax, one per line.")]
    public async Task<CallToolResult> ListDimensionValuesAsync(
        [Description("The maximum number of values to return.")]
        [Range(1, 1000)]
        int limit,
        [Description("An ISO 8601 timestamp specifying the lower bound for the search range.")]
        DateTimeOffset from,
        [Description("The upper bound for the search range.")]
        DateTimeOffset to,
        [Description("The dimension accessor, e.g. `cpu.mode`.")]
        string? dimension = null,
        CancellationToken cancellationToken = default)
    {
        var values = await connection.Metrics.ListDimensionValuesAsync(dimension, limit, from.UtcDateTime, to.UtcDateTime,
            session.DataToolCallTimeout, cancellationToken: cancellationToken);

        var result = new StringWriter();
        foreach (var value in values)
        {
            NativeFormatter.WriteValue(result, value);
            await result.WriteLineAsync();
        }

        return McpResults.SimpleText(result.ToString());
    }
}
