// Copyright Datalust Pty Ltd and Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Ingestion;
using Serilog;
using SerilogTracing;

namespace SeqCli.Sample.Loader;

static class Simulation
{
    public static async Task RunAsync(SeqConnection connection, string? apiKey, int batchSize, bool echoToStdout, CancellationToken cancellationToken = default)
    {
        var buffer = new BufferingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Origin", "seqcli sample ingest")
            .WriteTo.Conditional(_ => echoToStdout, wt => wt.Console())
            .WriteTo.Sink(buffer)
            .CreateLogger();

        var ship = Task.Run(() => LogShipper.ShipEvents(connection, apiKey, buffer,
            InvalidDataHandling.Fail, SendFailureHandling.Continue, batchSize));

        await Roastery.Program.Main(logger, cancellationToken);
        await logger.DisposeAsync();
        await ship;
    }
}