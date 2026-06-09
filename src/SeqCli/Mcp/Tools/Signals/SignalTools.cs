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

using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Seq.Api;

// ReSharper disable UnusedMember.Global

namespace SeqCli.Mcp.Tools.Signals;

[McpServerToolType]
class SignalTools(SeqConnection connection)
{
    [McpServerTool(Name = "seq_list_signals", ReadOnly = true, Title = "List Signals", UseStructuredContent = true)]
    [Description("List available signals. Use signals when searching and querying to efficiently work with well-known " +
                 "event streams while dramatically improving response times.")]
    public async Task<SignalSummary[]> ListSignalsAsync(CancellationToken cancellationToken)
    {
        return (await connection.Signals.ListAsync(shared: true, partial: true, cancellationToken: cancellationToken))
            .Select(s => new SignalSummary { Id = s.Id, Title = s.Title })
            .ToArray();
    }
}