// Copyright © Datalust Pty Ltd
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api.Model.Data;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.Metrics;

[Command("metrics", "search", "List available metric definitions",
    Example = "seqcli metrics search -f \"@Resource.service.name = 'proxy'\" -c 100")]
class SearchCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly DateRangeFeature _range;
    readonly StoragePathFeature _storagePath;
    string? _filter;
    readonly List<string> _groups = [];
    int _count = 1;
    bool _trace;

    public SearchCommand()
    {
        Options.Add(
            "f=|filter=",
            "A filter to apply to the search, including metric name/description text in double quotes, for example `\"cpu\" and Host = 'xmpweb-01.example.com'`",
            v => _filter = v);

        Options.Add(
            "g=|group=",
            "Group key for metric definition breakdown; this argument can be used multiple times",
            c => _groups.Add(ArgumentString.Normalize(c) ?? throw new ArgumentException("Group keys require a value.")));
        
        Options.Add(
            "c=|count=",
            $"The maximum number of metric definitions to retrieve; the default is {_count}",
            v => _count = int.Parse(v, CultureInfo.InvariantCulture));

        _range = Enable<DateRangeFeature>();
        // Native is not supported because accessor expressions appear in the output, and the escaping applied to them
        // as native strings does more harm than good.
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();

        Options.Add("trace", "Enable detailed (server-side) query tracing", _ => _trace = true);

        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        try
        {
            var config = RuntimeConfigurationLoader.Load(_storagePath);
            var output = _output.GetOutputFormat(config);
            var connection = SeqConnectionFactory.Connect(_connection, config);

            string? filter = null;
            if (!string.IsNullOrWhiteSpace(_filter))
                filter = (await connection.Expressions.ToStrictAsync(_filter)).StrictExpression;

            var result = await connection.Metrics.SearchAsync(
                _groups,
                filter,
                _count,
                rangeStartUtc: _range.Start,
                rangeEndUtc: _range.End,
                trace: _trace);
            
            // We convert the metric into a query result to improve formatting consistency. Room for an abstraction of
            // some kind here.
            var rows = new List<object?[]>();
            foreach (var metric in result.Metrics)
            {
                var row = new List<object?>
                {
                    metric.Accessor,
                    metric.Kind,
                    metric.Unit,
                    metric.Description
                };
                
                foreach (var value in metric.GroupKey)
                    row.Add(value);
                
                rows.Add(row.ToArray());
            }
            var asRowset = new QueryResultPart
            {
                Columns = new[] { "Accessor", "Kind", "Unit", "Description" }.Concat(_groups).ToArray(),
                Rows = rows.ToArray()
            };
            
            output.WriteQueryResult(asRowset);

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not retrieve metrics: {ErrorMessage}", ex.Message);
            return 1;
        }
    }
}