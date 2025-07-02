using System;
using System.IO;
using SeqCli.Config;
using Xunit;
using Xunit.Sdk;

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
    [EnvironmentOverridenConfigFilenameBeforeAfter(SeqCliConfigFile = "MyCustomSeqCli.json")]
    public void EnvironmentOverridenConfigFilename()
    {
        var configFile = RuntimeConfigurationLoader.SeqCliConfigFilename();
        var customConfigFile = Path.Combine(Path.GetTempPath(), "MyCustomSeqCli.json");
        Assert.Equal(customConfigFile, configFile);
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class EnvironmentOverridenConfigFilenameBeforeAfter : BeforeAfterTestAttribute
{
    private string originalValue;
    public string SeqCliConfigFile { get; set; }

    public override void Before(System.Reflection.MethodInfo methodUnderTest)
    {
        originalValue = Environment.GetEnvironmentVariable("SEQCLI_CONFIG_FILE");

        var configFile = Path.Combine(Path.GetTempPath(), SeqCliConfigFile);
        Environment.SetEnvironmentVariable("SEQCLI_CONFIG_FILE", configFile);
    }

    public override void After(System.Reflection.MethodInfo methodUnderTest)
    {
        Environment.SetEnvironmentVariable("SEQCLI_CONFIG_FILE", originalValue);
    }
}