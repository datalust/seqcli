using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;

namespace SeqCli.Cli.Commands.Bench;

class QueryBenchRunResults
{
    readonly Logger _reportingLogger;
    List<QueryBenchCaseTimings> _collectedTimings = new();

    public static QueryBenchCase FINAL_COUNT_CASE = new()
    {
        Id = "final-count-star",
        Query = "select count(*) from stream",
    };

    public QueryBenchRunResults(Logger reportingLogger)
    {
        _reportingLogger = reportingLogger;
    }

    public void Add(QueryBenchCaseTimings caseTimings)
    {
        _collectedTimings.Add(caseTimings);
    }

    public void LogSummary(string description)
    {
        _reportingLogger.Information(
            "Query benchmark {Description} complete in {TotalMeanElapsed:N0} ms with {MeanRelativeStandardDeviationPercentage:N1}% deviation, processed {FinalEventCount:N0} events at {EventsPerMs:N0} events/ms", 
            description,
            TotalMeanElapsed(),
            MeanRelativeStandardDeviationPercentage(),
            FinalEventCount(),
            FinalEventCount() * _collectedTimings.Count / TotalMeanElapsed());
    }

    private double TotalMeanElapsed()
    {
        return _collectedTimings.Sum(c => c.MeanElapsed);
    }

    private double MeanRelativeStandardDeviationPercentage()
    {
        return _collectedTimings.Average(c => c.RelativeStandardDeviationElapsed) * 100;
    }

    private long FinalEventCount()
    {
        var benchCase = _collectedTimings.Single(c => c.Id == FINAL_COUNT_CASE.Id);
        return Convert.ToInt64(benchCase.LastResult);
    }
}