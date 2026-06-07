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
using System.Globalization;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using Serilog;

namespace SeqCli.Cli.Commands.Metrics;

[Command("metrics", "dimensions", "List the dimensions associated with a given metric",
    Example = "seqcli metrics dimensions -m http.response.status_code")]
class DimensionsCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly DateRangeFeature _range;
    readonly StoragePathFeature _storagePath;
    string? _metric;
    int _count = 30;
    bool _trace;

    public DimensionsCommand()
    {
        Options.Add(
            "m=|metric=",
            "A metric name, for example `hats-sold` or `http.request.duration`; omit to list dimensions for all metrics",
            v => _metric= v);
        
        Options.Add(
            "c=|count=",
            $"The maximum number of dimensions to retrieve; the default is {_count}",
            v => _count = int.Parse(v, CultureInfo.InvariantCulture));

        _range = Enable<DateRangeFeature>();
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
            
            var result = await connection.Metrics.ListDimensionsAsync(
                _count,
                _metric,
                rangeStartUtc: _range.Start,
                rangeEndUtc: _range.End,
                trace: _trace);

            if (output.Json)
            {
                output.WriteObject(result);
            }
            else
            {
                foreach (var dimension in result)
                {
                    output.WriteText(dimension.Accessor);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not retrieve metrics: {ErrorMessage}", ex.Message);
            return 1;
        }
    }
}