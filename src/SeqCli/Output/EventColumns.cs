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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Nodes;
using Seq.Syntax.Expressions;
using SeqCli.Data;
using SeqCli.Syntax;

namespace SeqCli.Output;

/// <summary>
/// Evaluates a list of column expressions against each event, storing the results in synthetic properties that
/// the plain-text output template shows ahead of the message.
/// </summary>
class EventColumns : IEventEnricher
{
    static readonly string ColumnPrefixProperty = $"_SeqcliColumn_{Guid.NewGuid():N}";

    internal static string ColumnPropertyName(int index) => $"{ColumnPrefixProperty}_{index}";

    internal static string TemplateColumnsFragment(int columnCount)
    {
        // `<> ''` is undefined, and hence falsy, when the property is missing; the guard thus
        // drops the column, and its trailing space, for both missing and empty values.
        var fragment = new StringBuilder();
        for (var i = 0; i < columnCount; ++i)
        {
            var column = ColumnPropertyName(i);
            fragment.Append($"{{#if {column} <> ''}}{{{column}}} {{#end}}");
        }

        return fragment.ToString();
    }

    readonly CompiledExpression[] _columns;

    EventColumns(CompiledExpression[] columns)
    {
        _columns = columns;
    }

    public static bool TryCreate(
        IReadOnlyList<string> expressions,
        [NotNullWhen(true)] out EventColumns? columns,
        [NotNullWhen(false)] out string? error)
    {
        var compiled = new CompiledExpression[expressions.Count];
        for (var i = 0; i < expressions.Count; ++i)
        {
            if (!SeqSyntax.TryCompileExpression(expressions[i], out var expression, out error))
            {
                columns = null;
                return false;
            }

            compiled[i] = expression;
        }

        columns = new EventColumns(compiled);
        error = null;
        return true;
    }

    public string OutputTemplate() => TextFormatters.PlainOutputTemplate(_columns.Length);

    public void Enrich(JsonObject eventJson)
    {
        for (var i = 0; i < _columns.Length; ++i)
        {
            // Property accessors return nodes still attached to the event, so they're cloned before being
            // re-parented.
            if (_columns[i](eventJson).TryGetValue(out var value))
                eventJson[ColumnPropertyName(i)] = value?.DeepClone();
        }
    }
}
