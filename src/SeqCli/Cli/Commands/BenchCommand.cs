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

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seq.Api.Model.Signals;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;
using Serilog.Context;
using Serilog.Core;

namespace SeqCli.Cli.Commands;

[Command("bench", "Measure query performance")]
class BenchCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;
    int _runs = 0;
    readonly ConnectionFeature _connection;
    readonly DateRangeFeature _range;
    string _cases = "";
    string _reportingServerUrl = "";
    string _reportingServerApiKey = "";
    ILogger? _reportingLogger = null;
    
    const int DefaultRuns = 3;

    public BenchCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        Options.Add("r|runs=", "The number of runs to execute", r =>
        {
            if (!int.TryParse(r, out _runs))
            {
                _runs = DefaultRuns;
            }
        });
        
        Options.Add(
            "c|cases=", 
            "A JSON file containing the set of cases to run. Defaults to a standard set of cases", 
            c => _cases = c);

        _connection = Enable<ConnectionFeature>();
        _range = Enable<DateRangeFeature>();
        
        Options.Add(
            "reporting-server=", 
            "The address of a Seq server to send bench results to", 
            s => _reportingServerUrl = s);
        Options.Add(
            "reporting-apikey=", 
            "The API key to use when connecting to the reporting server", 
            a => _reportingServerApiKey = a);
    }
    
    protected override async Task<int> Run()
    {
        try
        {
            var connection = _connectionFactory.Connect(_connection);
            _reportingLogger = BuildReportingLogger();
        
            var cases = ReadCases(_cases);

            using (LogContext.PushProperty("BenchRunId", Guid.NewGuid()))
            {
                foreach (var c in cases.Cases)
                {
                    // TODO: collect results for each run
                    foreach (var i in Enumerable.Range(1, _runs))
                    {
                        var result = await connection.Data.QueryAsync(
                            c.Query,
                            _range.Start ?? DateTime.UtcNow.AddDays(-7),
                            _range.End,
                            SignalExpressionPart.FromIntersectedIds(c.Signals)
                        );

                        var isScalarResult = result.Rows.Length == 1 && result.Rows[0].Length == 1;
                        using (isScalarResult ? LogContext.PushProperty("Result", result.Rows[0][0]) : null)
                        using (LogContext.PushProperty("Query", c.Query))
                        {
                            _reportingLogger.Information("Benchmarked query {Id} in {Elapsed:N0}ms", c.Id,
                                result.Statistics.ElapsedMilliseconds);
                        }
                    }
                }
            }

            return 0;
        } catch (Exception ex)
        {
            Log.Error(ex, "Benchmarking failed: {ErrorMessage}", ex.Message);
            return 1;
        }
    }

    Logger BuildReportingLogger()
    {
        return string.IsNullOrWhiteSpace(_reportingServerUrl) 
            ? new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger()
            : new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq(_reportingServerUrl, period: TimeSpan.FromMilliseconds(1))
                .CreateLogger();
    }

    static BenchCasesCollection ReadCases(string filename)
    {
        var defaultCasesPath = Path.Combine(Path.GetDirectoryName(typeof(BenchCommand).Assembly.Location)!, "Cli/Commands/BenchCases.json");
        var casesString = File.ReadAllText(string.IsNullOrWhiteSpace(filename) 
            ? defaultCasesPath 
            : filename);
        return JsonConvert.DeserializeObject<BenchCasesCollection>(casesString)
            ?? new BenchCasesCollection();
    }
}

class BenchCasesCollection
{
    public IList<BenchCase> Cases = new List<BenchCase>();
}

/*
 * A benchmark test case.
 */
class BenchCase
{
    public string Id = "";
    public string Query = "";
    public IList<string> Signals = new List<string>();
}