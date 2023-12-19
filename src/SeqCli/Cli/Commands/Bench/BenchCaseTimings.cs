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

namespace SeqCli.Cli.Commands.Bench;

class CollectedTimings
{
    List<BenchCaseTimings> _collectedTimings = new();

    public static BenchCase FINAL_COUNT_CASE = new BenchCase()
    {
        Id = "final-count-star",
        Query = "select count(*) from stream",
    };

    public void Add(BenchCaseTimings caseTimings)
    {
        _collectedTimings.Add(caseTimings);
    }

    public void LogSummary()
    {
        throw new NotImplementedException();
    }
}

/*
 * Collects benchmarking elapsed time measurements and calculates statistics. 
 */
class BenchCaseTimings
{
    readonly BenchCase _benchCase;
    readonly List<double> _timings = new();
    
    public double MeanElapsed => _timings.Sum() / _timings.Count;
    public double MinElapsed => _timings.Min();
    public double MaxElapsed => _timings.Max();
    public double FirstElapsed => _timings.First();
    public double StandardDeviationElapsed => StandardDeviation(_timings); 
    public double RelativeStandardDeviationElapsed => StandardDeviation(_timings) / MeanElapsed;

    public BenchCaseTimings(BenchCase benchCase)
    {
        _benchCase = benchCase;
    }
    
    public void PushElapsed(double elapsed)
    {
        _timings.Add(elapsed);
    }

    static double StandardDeviation(ICollection<double> population)
    {
        if (population.Count < 2)
        {
            return 0;
        }
        
        var mean = population.Sum() / population.Count;
        return Math.Sqrt(population.Select(e => Math.Pow(e - mean, 2)).Sum() / (population.Count - 1));
    }
}