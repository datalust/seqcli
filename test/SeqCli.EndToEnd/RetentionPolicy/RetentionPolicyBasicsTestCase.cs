﻿using System;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.RetentionPolicy;

// ReSharper disable once UnusedType.Global
public class RetentionPolicyBasicsTestCase : ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var exit = runner.Exec("retention list");
        Assert.Equal(0, exit);

        exit = runner.Exec("retention list", "-i retentionpolicy-missing");
        Assert.Equal(1, exit);

        exit = runner.Exec("retention create", "--after 10h --delete-all-events");
        Assert.Equal(0, exit);

        var id = runner.LastRunProcess.Output.Trim();
        exit = runner.Exec("retention list", $"-i {id}");
        Assert.Equal(0, exit);

        var policy = await connection.RetentionPolicies.FindAsync(id);
        Assert.Null(policy.RemovedSignalExpression);
        Assert.Equal(TimeSpan.FromHours(10), policy.RetentionTime);

        exit = runner.Exec("retention remove", $"-i {id}");
        Assert.Equal(0, exit);
            
        exit = runner.Exec("retention list", $"-i {id}");
        Assert.Equal(1, exit);
        
        var deleteSignal = "signal-m33303,(signal-m33301~signal-m33302)";
        exit = runner.Exec("retention create", $"--after 10h --delete \"{deleteSignal}\"");
        Assert.Equal(0, exit);

        exit = runner.Exec("retention create", "--after 10h");
        Assert.Equal(1, exit);
        
        exit = runner.Exec("retention create", $"--after 10h --delete-all-events --delete \"{deleteSignal}\"");
        Assert.Equal(1, exit);
    }
}