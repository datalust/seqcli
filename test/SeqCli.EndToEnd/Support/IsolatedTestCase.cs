﻿using System;
using System.Threading.Tasks;
using Seq.Api;
using Serilog;

namespace SeqCli.EndToEnd.Support;

class IsolatedTestCase
{
    readonly Lazy<ITestProcess> _serverProcess;
    readonly Lazy<SeqConnection> _connection;
    readonly Lazy<ILogger> _logger;
    readonly CliCommandRunner _commandRunner;
    readonly Lazy<LicenseSetup> _licenseSetup;
    readonly ICliTestCase _testCase;

    ITestProcess _lastRunProcess;

    public IsolatedTestCase(
        Lazy<ITestProcess> serverProcess, 
        Lazy<SeqConnection> connection,
        Lazy<ILogger> logger,
        CliCommandRunner commandRunner,
        Lazy<LicenseSetup> licenseSetup,
        ICliTestCase testCase,
        TestConfiguration configuration)
    {
        _serverProcess = serverProcess;
        _connection = connection;
        _logger = logger;
        _commandRunner = commandRunner;
        _licenseSetup = licenseSetup;
        _testCase = testCase ?? throw new ArgumentNullException(nameof(testCase));
        Configuration = configuration;
    }

    public string Description => _testCase.GetType().Name;
    public string Output => _commandRunner.LastRunProcess?.Output ??_lastRunProcess?.Output ?? "<no process was run>";
    public TestConfiguration Configuration { get; }

    void ForceStartup()
    {
        _lastRunProcess = _serverProcess.Value;
    }

    public async Task<bool> IsSupportedApiVersion(string minSeqVersion)
    {
        ForceStartup();
            
        await _connection.Value.EnsureConnectedAsync(TimeSpan.FromSeconds(30));

        var apiVersion = (await _connection.Value.Client.GetRootAsync()).Version;

        // Ignore `-pre` and other semver modifiers.
        System.Version ParseVersion(string semver) => System.Version.Parse(semver.Split("-")[0]);

        return ParseVersion(apiVersion) >= ParseVersion(minSeqVersion);
    }

    public async Task ExecuteTestCaseAsync()
    {
        ForceStartup();
            
        await _connection.Value.EnsureConnectedAsync(TimeSpan.FromSeconds(30));
        await _licenseSetup.Value.SetupAsync(_connection.Value, _logger.Value);
        await _testCase.ExecuteAsync(_connection.Value, _logger.Value, _commandRunner);
    }
}