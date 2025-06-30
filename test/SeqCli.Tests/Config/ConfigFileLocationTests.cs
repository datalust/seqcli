using System;
using System.IO;
using SeqCli.Config;
using Xunit;

namespace SeqCli.Tests.Config;

public class ConfigFileLocationTests
{
    [Fact]
    public void DefaultConfigFilename()
    {
        var configFile = RuntimeConfigurationLoader.SeqCliConfigFilename();

        Assert.Equal(configFile, RuntimeConfigurationLoader.DefaultConfigFilename);
    }

    [Fact]
    public void EnvironmentOverridenConfigFilename()
    {
        var tempConfigFile = Path.GetTempFileName();
        Environment.SetEnvironmentVariable("SEQCLI_CONFIG_FILE", tempConfigFile);
        var configFile = RuntimeConfigurationLoader.SeqCliConfigFilename();
        // Clean up immediately to avoid affecting environment for other tests
        // TODO: Or better move to public void Dispose() ?
        Environment.SetEnvironmentVariable("SEQCLI_CONFIG_FILE", null);

        Assert.Equal(tempConfigFile, configFile);
    }
}