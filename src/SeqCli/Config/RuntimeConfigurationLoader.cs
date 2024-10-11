using System;
using System.IO;

namespace SeqCli.Config;

static class RuntimeConfigurationLoader
{
    public static readonly string DefaultConfigFilename =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SeqCli.json");

    const string DefaultEnvironmentVariablePrefix = "SEQCLI_";
    
    /// <summary>
    /// This is the method to use when loading configuration for runtime use. It will read the default configuration
    /// file, if any, and apply overrides from the environment.
    /// </summary>
    public static SeqCliConfig Load()
    {
        var config = SeqCliConfig.ReadFromFile(DefaultConfigFilename);
        
        EnvironmentOverrides.Apply(DefaultEnvironmentVariablePrefix, config);
            
        return config;
    }    
}