// Copyright 2018 Datalust Pty Ltd
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
using System.Text;

namespace SeqCli.Syntax;

class QueryBuilder
{
    readonly List<(string, string)> _columns = new();
    readonly List<string> _where = new();
    readonly List<string> _groupBy = new();
    readonly List<string> _having = new();

    public void Select(string value, string label)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        _columns.Add((value, label));
    }

    public bool FromStream { get; set; }

    public void Where(string predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        _where.Add(predicate);
    }

    public void GroupBy(string grouping)
    {
        if (grouping == null) throw new ArgumentNullException(nameof(grouping));
        _groupBy.Add(grouping);
    }

    public void GroupBy(TimeSpan interval)
    {
        _groupBy.Add($"time({DurationMoniker.FromTimeSpan(interval)})");
    }

    public void Having(string predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        _having.Add(predicate);
    }

    public int? Limit { get; set; }

    public string Build()
    {
        var result = new StringBuilder(140);

        result.Append("select");
        var selectDelim = " ";
        foreach (var (value, label) in _columns)
        {
            result.Append($"{selectDelim}{value}");
            selectDelim = ", ";

            if (label != null)
                result.Append($" as {label}");
        }

        if (FromStream)
            result.Append(" from stream");

        if (_where.Any())
        {
            var builder = new CombinedFilterBuilder();

            foreach (var where in _where)
                builder.Intersect(where);

            result.Append($" where {builder.Build()}");
        }

        if (_groupBy.Any())
        {
            result.Append(" group by");

            var groupDelim = " ";
            foreach (var grouping in _groupBy)
            {
                result.Append($"{groupDelim}{grouping}");
                groupDelim = ", ";
            }
        }

        if (_having.Any())
        {
            var builder = new CombinedFilterBuilder();

            foreach (var having in _having)
                builder.Intersect(having);

            result.Append($" having {builder.Build()}");
        }

        if (Limit.HasValue)
            result.Append($" limit {Limit}");

        return result.ToString();
    }
}