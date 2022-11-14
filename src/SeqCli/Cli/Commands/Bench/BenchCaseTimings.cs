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

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SeqCli.Cli.Commands;

/*
 * Collects benchmarking elapsed time measurements and calculates statistics. 
 */
class BenchCaseTimings
{
    readonly List<double> _elaspseds = new() { };
    public double MeanElapsed => _elaspseds.Sum() / _elaspseds.Count;
    public double MinElapsed => _elaspseds.Min();
    public double MaxElapsed => _elaspseds.Max();
    public double StandardDeviationElapsed => StandardDeviation(_elaspseds); 
    public double RelativeStandardDeviationElapsed => StandardDeviation(_elaspseds) / MeanElapsed;

    public void PushElapsed(double elapsed)
    {
        _elaspseds.Add(elapsed);
    }

    double StandardDeviation(IList<double> population)
    {
        if (population.Count < 2)
        {
            return 0;
        }
        var mean = population.Sum() / population.Count;
        return Math.Sqrt(population.Select(e => Math.Pow(e - mean, 2)).Sum() / (population.Count - 1));
    }
}