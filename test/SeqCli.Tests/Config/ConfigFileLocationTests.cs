using System.Collections.Generic;
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
        var customConfigFile = Path.GetTempFileName();
        var environment = new Dictionary<string, string>();
        environment.Add("SEQCLI_CONFIG_FILE", customConfigFile);

        var configFile = RuntimeConfigurationLoader.SeqCliConfigFilename(environment);
        Assert.Equal(customConfigFile, configFile);
    }
}
