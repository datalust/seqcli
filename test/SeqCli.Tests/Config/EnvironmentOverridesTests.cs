using System.Collections.Generic;
using SeqCli.Config;
using Xunit;

namespace SeqCli.Tests.Config;

public class EnvironmentOverridesTests
{
    [Fact]
    public void EnvironmentVariableOverridesAreApplied()
    {
        const string initialUrl = "https://old.example.com";

        var config = new SeqCliConfig
        {
            Connection =
            {
                ServerUrl = initialUrl
            }
        };

        var environment = new Dictionary<string, string>();
        EnvironmentOverrides.Apply("SEQCLI_", config, environment);

        Assert.Equal(initialUrl, config.Connection.ServerUrl);

        const string updatedUrl = "https://new.example.com";
        environment["SEQCLI_CONNECTION_SERVERURL"] = updatedUrl;
        EnvironmentOverrides.Apply("SEQCLI_", config, environment);

        Assert.Equal(updatedUrl, config.Connection.ServerUrl);
    }
}
