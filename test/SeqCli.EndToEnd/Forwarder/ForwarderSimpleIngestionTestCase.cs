using System;
using System.Globalization;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Forwarder;

public class ForwarderSimpleIngestionTestCase: ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var (forwarder, forwarderUri) = await runner.SpawnForwarderAsync();
        using (forwarder)
        {
            var ingestionLogger = new LoggerConfiguration()
                .WriteTo.Seq(forwarderUri)
                .CreateLogger();

            const int itemCount = 1032;
            for (var i = 0; i < itemCount; ++i)
            {
                ingestionLogger.ForContext("Ballast", new string('a', 51))
                    .Information("At item {I}", i);
            }

            // In recent versions this should be sufficient to flush any queued events.
            await ingestionLogger.DisposeAsync();

            // Give forwarder enough time to move data...
            await Task.Delay(TimeSpan.FromSeconds(5));

            var result = await connection.Data.QueryAsync("select count(*) from stream");
            var retrievedCount = (long)result.Rows[0][0];
            
            Assert.Equal(itemCount, retrievedCount);
        }
    }
}
