using System;
using System.Threading.Tasks;
using Seq.Api;
using Serilog;
using Xunit;

namespace SeqCli.Tests.Integration.Support
{
    
    public class CliTestCase : IDisposable, IAsyncLifetime
    {
        internal SeqConnection _connection;
        internal CliCommandRunner _runner;
        TestDataFolder _testDataFolder;
        CaptiveProcess _seqServerProcess;

        public async Task InitializeAsync()
        {
            var testConfiguration = new TestConfiguration();
            _testDataFolder = new TestDataFolder();
            _seqServerProcess = testConfiguration.SpawnServerProcess(_testDataFolder.Path);
            _connection = new SeqConnection(testConfiguration.ServerListenUrl);
            await _connection.EnsureConnected();
            
            _runner = new CliCommandRunner(testConfiguration);
        }

        public Task DisposeAsync()
        {
            using (_connection)
            using (_seqServerProcess)
            using (_testDataFolder)
            {}
            
            var exitCode = _seqServerProcess.WaitForExit(TimeSpan.FromSeconds(10));

            if (exitCode != 0)
            {
                Console.Error.WriteLineAsync(_seqServerProcess.Output);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            
        }
    }
}
