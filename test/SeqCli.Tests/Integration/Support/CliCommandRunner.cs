using System;

namespace SeqCli.Tests.Integration.Support
{
    public class CliCommandRunner
    {
        readonly TestConfiguration _configuration;
        static readonly TimeSpan DefaultExecTimeout = TimeSpan.FromSeconds(10);
        
        public ITestProcess LastRunProcess { get; private set; }

        public CliCommandRunner(TestConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int Exec(string command, string args = null, bool disconnected = false)
        {
            using (var process = _configuration.SpawnCliProcess(command, args, skipServerArg: disconnected))
            {
                LastRunProcess = process;
                return process.WaitForExit(DefaultExecTimeout);
            }
        }
    }
}