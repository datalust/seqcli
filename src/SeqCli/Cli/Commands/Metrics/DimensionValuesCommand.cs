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

[Command("metrics", "dimensionvalues", "List distinct values for a metric dimension",
    Example = "seqcli metrics dimensionvalues --accessor @Resource.service.name")]
class DimensionValuesCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly DateRangeFeature _range;
    readonly StoragePathFeature _storagePath;
    string? _accessor;
    int _count = 30;
    bool _trace;

    public DimensionValuesCommand()
    {
        Options.Add(
            "d=|accessor=",
            "The dimension accessor, e.g. `cpu.mode`",
            v => _accessor= v);
        
        Options.Add(
            "c=|count=",
            $"The maximum number of dimensions to retrieve; the default is {_count}",
            v => _count = int.Parse(v, CultureInfo.InvariantCulture));

        _range = Enable<DateRangeFeature>();
        _output = Enable(new OutputFormatFeature(supportNative: true));
        _storagePath = Enable<StoragePathFeature>();

        Options.Add("trace", "Enable detailed (server-side) query tracing", _ => _trace = true);

        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_accessor))
            {
                Log.Error("A dimension `--accessor` must be specified");
                return 1;
            }

            var config = RuntimeConfigurationLoader.Load(_storagePath);
            var output = _output.GetOutputFormat(config);
            var connection = SeqConnectionFactory.Connect(_connection, config);
            
            var result = await connection.Metrics.ListDimensionValuesAsync(
                _accessor,
                _count,
                rangeStartUtc: _range.Start,
                rangeEndUtc: _range.End,
                trace: _trace);

            if (output.Json)
            {
                // In the JSON case we write an array with all values.
                output.WriteObject(result);
            }
            else
            {
                // Native and plain text formatting use one-per-line output (both allow multi-line strings, but
                // string boundaries are clearer in native mode).
                foreach (var value in result)
                {
                    output.WriteObject(value);
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