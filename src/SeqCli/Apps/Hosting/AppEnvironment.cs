using System;
using System.Collections.Generic;

namespace SeqCli.Apps.Hosting;

class AppEnvironment
{
    AppEnvironment(
        string appInstanceId,
        string appInstanceTitle,
        string storagePath,
        string serverUrl,
        string serverInstanceName,
        IReadOnlyDictionary<string, string> settings)
    {
        AppInstanceId = appInstanceId ?? throw new ArgumentNullException(nameof(appInstanceId));
        AppInstanceTitle = appInstanceTitle ?? throw new ArgumentNullException(nameof(appInstanceTitle));
        StoragePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
        ServerUrl = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
        SeqInstanceName = serverInstanceName ?? throw new ArgumentNullException(nameof(serverInstanceName));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public static AppEnvironment ReadStandardEnvironment()
    {
        return new AppEnvironment(
            GetEnvironmentVariable("SEQ_APP_ID"),
            GetEnvironmentVariable("SEQ_APP_TITLE"),
            GetEnvironmentVariable("SEQ_APP_STORAGEPATH"),
            GetEnvironmentVariable("SEQ_INSTANCE_BASEURI"),
            GetEnvironmentVariable("SEQ_INSTANCE_NAME"),
            ReadSettingsFromEnvironment());
    }

    static string GetEnvironmentVariable(string name)
    {
        var var = Environment.GetEnvironmentVariable(name);
        if (var == null) throw new Exception($"The `{name}` environment variable is not set.");
        return var;
    }

    static IReadOnlyDictionary<string, string> ReadSettingsFromEnvironment()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var variables = Environment.GetEnvironmentVariables();
        foreach (string key in variables.Keys)
        {
            if (key.StartsWith("SEQ_APP_SETTING_"))
                result.Add(key.Substring(16), (string)variables[key]!);
        }
        return result;
    }

    public IReadOnlyDictionary<string, string> Settings { get; }
    public string StoragePath { get; }
    public string ServerUrl { get; }
    public string AppInstanceId { get; }
    public string AppInstanceTitle { get; }
    public string SeqInstanceName { get; }
}