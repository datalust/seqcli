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
using System.Threading.Tasks;
using Seq.Api;
using Seq.Api.Model.Signals;
using SeqCli.Signals;
using SeqCli.Syntax;
using SeqCli.Util;

namespace SeqCli.Cli.Features;

class EventColumnsFeature : CommandFeature
{
    readonly List<string> _columns = [];
    bool _noSignalColumns;

    public override void Enable(OptionSet options)
    {
        options.Add(
            "column=",
            "A column to display preceding each event's message; any Seq expression can be supplied, for " +
            "example `OrderId`, `@SpanKind`, or `@Resource.service.name`; this argument can be used multiple " +
            "times, adding columns in order; applies to plain-text output only",
            c => _columns.Add(ArgumentString.Normalize(c) ?? throw new ArgumentException("Columns require a value.")));

        options.Add(
            "no-signal-columns",
            "Do not show columns associated with the specified signal expression",
            _ => _noSignalColumns = true);
    }

    public async Task<IReadOnlyList<string>> GetColumns(SeqConnection connection, SignalExpressionPart? signal)
    {
        var columns = new List<string>();
        if (!_noSignalColumns && signal is { } signalExpression)
        {
            foreach (var signalId in signalExpression.ReferencedSignalIds())
            {
                var signalEntity = await connection.Signals.FindAsync(signalId);
                foreach (var column in signalEntity.Columns)
                {
                    columns.Add(column.Expression);
                }
            }
        }

        columns.AddRange(_columns);

        foreach (var column in columns)
        {
            // A better error than a failed output template parse.
            if (!SeqSyntax.TryCompileExpression(column, out _, out var error))
                throw new ArgumentException($"The column expression `{column}` could not be compiled: {error}");
        }

        return columns;
    }
}
