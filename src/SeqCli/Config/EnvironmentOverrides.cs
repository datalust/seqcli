using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeqCli.Config;

static class EnvironmentOverrides
{
    public static void Apply(string prefix, SeqCliConfig config)
    {
        var environment = Environment.GetEnvironmentVariables();
        Apply(prefix, config, environment.Keys.Cast<string>().ToDictionary(k => k, k => (string?)environment[k]));
    }

    internal static void Apply(string prefix, SeqCliConfig config, Dictionary<string, string?> environment)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (environment == null) throw new ArgumentNullException(nameof(environment));

        config.DisallowExport();

        foreach (var (key, _, _) in KeyValueSettings.Inspect(config))
        {
            var envVar = ToEnvironmentVariableName(prefix, key);
            if (environment.TryGetValue(envVar, out var value))
            {
                KeyValueSettings.Set(config, key, value ?? "");
            }
        }
    }

    internal static string ToEnvironmentVariableName(string prefix, string key)
    {
        return prefix + key.Replace(".", "_").ToUpperInvariant();
    }
}
