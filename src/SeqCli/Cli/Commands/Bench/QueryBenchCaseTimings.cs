// Copyright Datalust Pty Ltd and Contributors
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
using Serilog.Core;

namespace SeqCli.Cli.Commands.Bench;

class CollectedTimings
{
    readonly Logger _reportingLogger;
    List<QueryBenchCaseTimings> _collectedTimings = new();

    public static QueryBenchCase FINAL_COUNT_CASE = new QueryBenchCase()
    {
        Id = "final-count-star",
        Query = "select count(*) from stream",
    };

    public CollectedTimings(Logger reportingLogger)
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

    private int FinalEventCount()
    {
        var benchCase = _collectedTimings.Single(c => c.Id == FINAL_COUNT_CASE.Id);
        return Convert.ToInt32(benchCase.LastResult);
    }
}

/*
 * Collects benchmarking elapsed time measurements and calculates statistics. 
 */
class QueryBenchCaseTimings
{
    readonly QueryBenchCase _queryBenchCase;
    readonly List<double> _timings = new();
    object? _lastResult;

    public double MeanElapsed => _timings.Sum() / _timings.Count;
    public double MinElapsed => _timings.Min();
    public double MaxElapsed => _timings.Max();
    public double FirstElapsed => _timings.First();
    public double StandardDeviationElapsed => StandardDeviation(_timings); 
    public double RelativeStandardDeviationElapsed => StandardDeviation(_timings) / MeanElapsed;

    public object? LastResult
    {
        get => _lastResult;
        set => _lastResult = value;
    }

    public QueryBenchCaseTimings(QueryBenchCase queryBenchCase)
    {
        _queryBenchCase = queryBenchCase;
    }
    
    public void PushElapsed(double elapsed)
    {
        _timings.Add(elapsed);
    }

    public static double StandardDeviation(ICollection<double> population)
    {
        if (population.Count < 2)
        {
            return 0;
        }
        
        var mean = population.Sum() / population.Count;
        return Math.Sqrt(population.Select(e => Math.Pow(e - mean, 2)).Sum() / (population.Count - 1));
    }

    public string Id => _queryBenchCase.Id;
}