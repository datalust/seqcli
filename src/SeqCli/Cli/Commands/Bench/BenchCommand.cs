// Copyright © Datalust Pty Ltd and Contributors
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seq.Api;
using Seq.Api.Model.Data;
using Seq.Api.Model.Signals;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Sample.Loader;
using SeqCli.Util;
using Serilog;
using Serilog.Context;
using Serilog.Core;

namespace SeqCli.Cli.Commands.Bench;

/*
 * Run performance benchmark tests against a Seq server.
 *
 * Requires test cases in a JSON file matching the format of `BenchCases.json`.
 * 
 * If a Seq reporting server is configured this command logs data such as:
 * 
 * {
    "@t": "2022-11-09T01:12:06.0293545Z",
    "@mt": "Bench run {BenchRunId} for query {Id}. Mean {MeanElapsed:N0} ms with relative dispersion {RelativeStandardDeviationElapsed:N2}",
    "@m": "Bench run \"2907\" for query \"with-signal\". Mean 4 ms with relative dispersion 0.06",
    "@i": "bb0c84a5",
    "@r": [
        "4",
        "0.06"
    ],
    "BenchRunId": "2907",
    "End": "2022-08-15T00:00:00.0000000",
    "Id": "with-signal",
    "LastResult": 606,
    "MaxElapsed": 4.082,
    "MeanElapsed": 3.6676,
    "MinElapsed": 3.4334,
    "Query": "select count(*) from stream where @Level = 'Warning'",
    "RelativeStandardDeviationElapsed": 0.05619408341421253,
    "Runs": 10,
    "SignalExpression": "signal-m33302",
    "Start": "2022-08-14T16:00:00.0000000"
}
 */
[Command("bench", @"Measure query performance")]
class BenchCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;
    int _runs = 10;
    readonly ConnectionFeature _connection;
    readonly DateRangeFeature _range;
    readonly TimeoutFeature _timeout;
    readonly StoragePathFeature _storagePath;
    string _cases = "";
    string _reportingServerUrl = "";
    string _reportingServerApiKey = "";
    string _description = "";
    bool _withIngestion = false;
    bool _withQueries = false;

    public BenchCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        Options.Add("r|runs=", "The number of runs to execute; the default is 10", r => _runs = int.Parse(r));
        
        Options.Add(
            "c|cases=", 
            @"A JSON file containing the set of cases to run. Defaults to a standard set of cases.", 
            c => _cases = c);

        _connection = Enable<ConnectionFeature>();
        _range = Enable<DateRangeFeature>();
        _timeout = Enable<TimeoutFeature>();
        
        Options.Add(
            "reporting-server=", 
            "The address of a Seq server to send bench results to", 
            s => _reportingServerUrl = s);
        Options.Add(
            "reporting-apikey=", 
            "The API key to use when connecting to the reporting server", 
            a => _reportingServerApiKey = a);
        Options.Add(
            "description=", 
            "Optional description of the bench test run", 
            a => _description = a);
        Options.Add(
            "with-ingestion",
            "Should the benchmark include sending events to Seq",
            _ => _withIngestion = true);
        Options.Add(
            "with-queries",
            "Should the benchmark include querying Seq",
            _ => _withQueries = true);

        _storagePath = Enable<StoragePathFeature>();
    }
    
    protected override async Task<int> Run()
    {
        if (!_withIngestion && !_withQueries)
        {
            Log.Error("Use at least one of --with-ingestion and --with-queries");
            return 1;
        }
        
        try
        {
            var config = RuntimeConfigurationLoader.Load(_storagePath);
            var (_, apiKey) = _connectionFactory.GetConnectionDetails(_connection, config);
            var connection = _connectionFactory.Connect(_connection, config);
            var timeout = _timeout.ApplyTimeout(connection.Client.HttpClient);
            var seqVersion = (await connection.Client.GetRootAsync()).Version;
            await using var reportingLogger = BuildReportingLogger();

            var runId = Guid.NewGuid().ToString("N")[..16];
            CancellationTokenSource cancellationTokenSource = new ();
            var cancellationToken = cancellationTokenSource.Token;
            
            using (LogContext.PushProperty("RunId", runId))
            using (LogContext.PushProperty("SeqVersion", seqVersion))
            using (LogContext.PushProperty("WithIngestion", _withIngestion))
            using (LogContext.PushProperty("WithQueries", _withQueries))
            using (LogContext.PushProperty("Start", _range.Start))
            using (LogContext.PushProperty("End", _range.End))
            using (!string.IsNullOrWhiteSpace(_description)
                       ? LogContext.PushProperty("Description", _description)
                       : null)
            {
                if (_withIngestion)
                {
                    var t = IngestionBenchmark(reportingLogger, runId, connection, apiKey, seqVersion,
                        isQueryBench: _withQueries, cancellationToken)
                        .ContinueWith(t =>
                        {
                            if (t.Exception is not null)
                            {
                                return Console.Error.WriteLineAsync(t.Exception.Message);
                            }

                            return Task.CompletedTask;
                        });

                    if (!_withQueries)
                    {
                        const int benchDurationMs = 120_000;
                        await Task.Delay(benchDurationMs, cancellationToken);
                        await cancellationTokenSource.CancelAsync();
                                
                        var response = await connection.Data.QueryAsync(
                            "select count(*) from stream group by time(1s)",
                            DateTime.Now.Add(-1 * TimeSpan.FromMilliseconds(benchDurationMs))
                        );
                                
                        if (response.Slices == null)
                        {
                            throw new Exception("Failed to query ingestion benchmark results");
                        }
                                
                        var counts = response.Slices.Skip(30) // ignore the warmup
                            .Select(s => Convert.ToDouble(s.Rows[0][0])) // extract per-second counts
                            .Where(c => c > 10000) // ignore any very small values
                            .ToArray();
                        counts = counts.SkipLast(5).ToArray(); // ignore warmdown
                        var countsMean = counts.Sum() / counts.Length;
                        var countsRSD = QueryBenchCaseTimings.StandardDeviation(counts) / countsMean;
                                
                        using (LogContext.PushProperty("EventsPerSecond", counts))
                        {
                            reportingLogger.Information(
                                "Ingestion benchmark {Description} ran for {RunDuration:N0}ms; ingested {TotalIngested:N0} " 
                                + "at {EventsPerMinute:N0}events/min; with RSD {RelativeStandardDeviationPercentage,4:N1}%",
                                _description,
                                benchDurationMs,
                                counts.Sum(),
                                countsMean * 60,
                                countsRSD * 100);
                        }
                    }
                }

                if (_withQueries)
                {
                    var collectedTimings = await QueryBenchmark(reportingLogger, runId, connection, seqVersion, timeout);
                    collectedTimings.LogSummary(_description);
                    await cancellationTokenSource.CancelAsync();
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Benchmarking failed: {ErrorMessage}", ex.Message);
            return 1;
        }
    }

    static async Task IngestionBenchmark(Logger reportingLogger, string runId, SeqConnection connection, string? apiKey, 
        string seqVersion, bool isQueryBench, CancellationToken cancellationToken = default)
    {
        reportingLogger.Information(
            "Ingestion bench run {RunId} against {ServerUrl} ({SeqVersion})",
            runId, connection.Client.ServerUrl, seqVersion);

        if (isQueryBench)
        {
            var simulationTasks = Enumerable.Range(1, 500)
                .Select(i => Simulation.RunAsync(connection, apiKey, 10000, echoToStdout: false, cancellationToken))
                .ToArray();
            await Task.Delay(20_000, cancellationToken); // how long to ingest before beginning queries
        }
        else
        {
            var simulationTasks = Enumerable.Range(1, 1000)
                .Select(i => Simulation.RunAsync(connection, apiKey, 10000, echoToStdout: false, cancellationToken))
                .ToArray();
        }
    }

    async Task<QueryBenchRunResults> QueryBenchmark(Logger reportingLogger, string runId, SeqConnection connection,
        string seqVersion, TimeSpan? timeout)
    {
        var cases = ReadCases(_cases);
        QueryBenchRunResults queryBenchRunResults = new(reportingLogger);
        reportingLogger.Information(
            "Query benchmark run {RunId} against {ServerUrl} ({SeqVersion}); {CaseCount} cases, {Runs} runs, from {Start} to {End}",
            runId, connection.Client.ServerUrl, seqVersion, cases.Cases.Count, _runs, _range.Start, _range.End);
        

        foreach (var c in cases.Cases.OrderBy(c => c.Id)
                     .Concat([QueryBenchRunResults.FINAL_COUNT_CASE]))
        {
            var timings = new QueryBenchCaseTimings(c);
            queryBenchRunResults.Add(timings);

            foreach (var i in Enumerable.Range(1, _runs))
            {
                var response = await connection.Data.QueryAsync(
                    c.Query,
                    _range.Start,
                    _range.End,
                    c.SignalExpression != null ? SignalExpressionPart.Signal(c.SignalExpression) : null,
                    null,
                    timeout
                );
        
                timings.PushElapsed(response.Statistics.ElapsedMilliseconds);

                if (response.Rows != null)
                {
                    var isScalarResult = response.Rows.Length == 1 && response.Rows[0].Length == 1;
                    if (isScalarResult && i == _runs)
                    {
                        timings.LastResult = response.Rows[0][0];
                    }
                }
            }

            using (timings.LastResult != null ? LogContext.PushProperty("LastResult", timings.LastResult) : null)
            using (!string.IsNullOrWhiteSpace(c.SignalExpression)
                       ? LogContext.PushProperty("SignalExpression", c.SignalExpression)
                       : null)
            using (LogContext.PushProperty("StandardDeviationElapsed", timings.StandardDeviationElapsed))
            using (LogContext.PushProperty("Query", c.Query))
            {
                reportingLogger.Information(
                    "Case {Id,-40} ({LastResult}) mean {MeanElapsed,5:N0} ms (first {FirstElapsed,5:N0} ms, min {MinElapsed,5:N0} ms, max {MaxElapsed,5:N0} ms, RSD {RelativeStandardDeviationElapsed,4:N2})",
                    c.Id, timings.LastResult, timings.MeanElapsed, timings.FirstElapsed, timings.MinElapsed, timings.MaxElapsed, timings.RelativeStandardDeviationElapsed);
            }
        }

        return queryBenchRunResults;
    }

    /// <summary>
    /// Build a second Serilog logger for logging benchmark results. 
    /// </summary>
    Logger BuildReportingLogger()
    { 
        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console();

        if (!string.IsNullOrWhiteSpace(_reportingServerUrl))
            loggerConfiguration.WriteTo.Seq(
                _reportingServerUrl, 
                apiKey: string.IsNullOrWhiteSpace(_reportingServerApiKey) ? null : _reportingServerApiKey,
                period: TimeSpan.FromMilliseconds(1));

        return loggerConfiguration.CreateLogger();
    }

    /// <summary>
    /// Read and parse the bench test cases from the file supplied or else from a default file. 
    /// </summary>
    static BenchCasesCollection ReadCases(string filename)
    {
        var defaultCasesPath = Content.GetPath("Cli/Commands/Bench/BenchCases.json");
        var casesString = File.ReadAllText(string.IsNullOrWhiteSpace(filename)
            ? defaultCasesPath
            : filename);
        
        var casesFile = JsonConvert.DeserializeObject<BenchCasesCollection>(casesString)
                        ?? new BenchCasesCollection();

        if (casesFile.Cases.Select(c => c.Id).Distinct().Count() != casesFile.Cases.Count)
        {
            throw new ArgumentException($"Cases file `{filename}` contains a duplicate id.");
        }

        if (!casesFile.Cases.Any())
        {
            throw new ArgumentException($"Cases file `{filename}` contains no cases.");
        }

        return casesFile;
    }
}
