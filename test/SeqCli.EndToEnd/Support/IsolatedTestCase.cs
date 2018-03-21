using System;
using System.Threading.Tasks;
using Seq.Api;
using Serilog;

namespace SeqCli.EndToEnd.Support
{
    class IsolatedTestCase
    {
        readonly Lazy<ITestProcess> _serverProcess;
        readonly Lazy<SeqConnection> _connection;
        readonly Lazy<ILogger> _logger;
        readonly CliCommandRunner _commandRunner;
        readonly ICliTestCase _testCase;

        ITestProcess _lastRunProcess;

        public IsolatedTestCase(
            Lazy<ITestProcess> serverProcess, 
            Lazy<SeqConnection> connection,
            Lazy<ILogger> logger,
            CliCommandRunner commandRunner,
            ICliTestCase testCase)
        {
            _serverProcess = serverProcess;
            _connection = connection;
            _logger = logger;
            _commandRunner = commandRunner;
            _testCase = testCase ?? throw new ArgumentNullException(nameof(testCase));
        }

        public string Description => _testCase.GetType().Name;
        public string Output => _commandRunner.LastRunProcess?.Output ??_lastRunProcess?.Output ?? "<no process was run>";

        public async Task ExecuteTestCaseAsync()
        {
            _lastRunProcess = _serverProcess.Value;
            await _connection.Value.EnsureConnected();            
            await _testCase.ExecuteAsync(_connection.Value, _logger.Value, _commandRunner);
        }
    }
}
