// Copyright © Datalust and contributors.
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

using System.Collections.Generic;
using System.ComponentModel;

namespace SeqCli.Mcp.Tools.Metrics;

[Description("Describes a metric.")]
class MetricDefinition
{
    [Description("The metric name.")]
    public required string Name { get; init; }
    
    [Description("The metric kind; normally one of `Sum` (counters with delta temporality), `Gauge`, `Fixed` (fixed-bucket histogram), or `Exponential` (exponentially-bucketed histogram).")]
    public required string Kind { get; init; }
    
    [Description("The UCUM unit specified for the metric, if any.")]
    public string? Unit { get; init; }
    
    [Description("A human-readable description of the metric, if available.")]
    public string? Description { get; init; }

    [Description("If group keys were specified when retrieving the metric, the values of those group keys for this definition.")]
    public List<object?> GroupKeyValues { get; init; } = [];
}