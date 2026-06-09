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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Seq.Api;
using Seq.Api.Client;
using Seq.Api.Model.Data;
using Seq.Api.Model.Signals;
using SeqCli.Output;
using SeqCli.Signals;
using Serilog;

// ReSharper disable UnusedMember.Global

namespace SeqCli.Mcp.Tools.Query;

[McpServerToolType]
class QueryTools(McpSession session, SeqConnection connection)
{
    [McpServerTool(Name = "seq_query", ReadOnly = true, Title = "Evaluate a Query over Logs, Spans, or Metric Samples")]
    [Description("Evaluate a Seq query, producing tabular results. Use the `seq-search-and-query` " +
                 "skill when calling this tool.")]
    [return: Description("Query results and status information.")]
    public async Task<CallToolResult> QueryAsync(
        [Description("A Seq query language query.")]
        string query,
        [Description("A signal expression restricting the search space. Multiple " +
                     "signals are intersected with commas, and unioned with tilde, for example, `signal-1,(signal-2~signal-3)`.")]
        string? signal = null,
        CancellationToken cancellationToken = default)
    {
        if (query.Contains("from", StringComparison.OrdinalIgnoreCase) &&
            (!query.Contains("where", StringComparison.OrdinalIgnoreCase) ||
            !query.Contains("@Timestamp", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("@Id", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("@TraceId", StringComparison.OrdinalIgnoreCase)))
        {
            return McpResults.SimpleText("The query doesn't adequately constrain the search range (by `@Timestamp`, `@TraceId`, or `@Id`). " +
                                    "To avoid consuming excessive resources, add a time bound such as `where @Timestamp >= now() - 1d`.", isError: true);
        }

        if (query.Contains("series", StringComparison.OrdinalIgnoreCase) &&
            query.Contains("@Definitions", StringComparison.OrdinalIgnoreCase))
        {
            return McpResults.SimpleText("Queries over the `@Definitions` property are not currently permitted. Use dedicated metrics-oriented " +
                                         "tools or CLI commands to search for metric definitions.", isError: true);
        }

        SignalExpressionPart? parsedSignalExpression = null;
        if (!string.IsNullOrWhiteSpace(signal))
            parsedSignalExpression = SignalExpressionParser.ParseExpression(signal);
        
        QueryResultPart result;
        try
        {
            result = await connection.Data.TryQueryAsync(query, signal: parsedSignalExpression, timeout: session.DataToolCallTimeout, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex.GetBaseException() is not OperationCanceledException)
            {
                Log.Error(ex, "Exception thrown during query execution");
            }
            
            var error = ex.GetBaseException() is SeqApiException ? ex.GetBaseException().Message : ex.ToString();
            return McpResults.SimpleText($"The query failed. {error}", isError: true);
        }
        
        var output = new StringWriter();
        NativeFormatter.WriteQueryResult(output, result);
        return McpResults.SimpleText(output.ToString(), isError: !string.IsNullOrWhiteSpace(result.Error));
    }
}
