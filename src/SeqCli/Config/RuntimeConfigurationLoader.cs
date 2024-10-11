using System;
using System.IO;

namespace SeqCli.Config;

static class RuntimeConfigurationLoader
{
    public static readonly string DefaultConfigFilename =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SeqCli.json");

    const string DefaultEnvironmentVariablePrefix = "SEQCLI_";
    
    /// <summary>
    /// This is the method to use when loading configuration for runtime use. It will apply overrides from the
    /// secret store and environment, and validate the configuration.
    /// </summary>
    public static SeqCliConfig Load()
    {
        var config = File.Exists(DefaultConfigFilename) ?
            SeqCliConfig.ReadFromFile(DefaultConfigFilename) :
            new SeqCliConfig();
        
        EnvironmentOverrides.Apply(DefaultEnvironmentVariablePrefix, config);
            
        return config;
    }    
}