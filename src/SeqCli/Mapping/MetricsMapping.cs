using System;

namespace SeqCli.Mapping;

public static class MetricsMapping
{
    internal static readonly string SurrogateDefinitionsProperty = $"_SeqcliMetricDefinitions_{Guid.NewGuid():N}";
    internal static readonly string SurrogateSamplesProperty = $"_SeqcliMetricSamples_{Guid.NewGuid():N}";
}
