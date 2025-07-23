using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Forwarder;

public class ForwarderIngestionValidationTestCase: ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection _, ILogger logger, CliCommandRunner runner)
    {
        const int eventSizeLimit = 10_000;
        const int batchSizeLimit = eventSizeLimit * 10;
        var (forwarder, forwarderUri) = await runner.SpawnForwarderAsync(environment: new()
        {
            ["SEQCLI_CONNECTION_EVENTSIZELIMITBYTES"] = eventSizeLimit.ToString(CultureInfo.InvariantCulture),
            ["SEQCLI_CONNECTION_BATCHSIZELIMITBYTES"] = batchSizeLimit.ToString(CultureInfo.InvariantCulture)
        });
        using (forwarder)
        {
            using var connection = new SeqConnection(forwarderUri);
            
            var isoNow = DateTime.UtcNow.ToString("o");

            await IngestClef(connection, $"{{\"@t\":\"{isoNow}\"}}", HttpStatusCode.Created);
            await IngestClef(connection, $"{{\"@t\":\"{isoNow}\",\"\":42}}", HttpStatusCode.Created);
            await IngestClef(connection, $"{{\"@t\":\"{isoNow}\",\"@mt\":\"{{a.b}}{{a.c}}{{b.d}}\",\"a\":{{\"b\":1}}}}",
                HttpStatusCode.Created);
            await IngestClef(connection,
                $"{{\"@t\":\"{isoNow}\",\"@mt\":\"test {{a}} {{b}}\",\"@i\":\"ba6bd213\",\"a\":\"A\",\"b\":42}}",
                HttpStatusCode.Created);
            await IngestClef(connection, $"{{\"@t\":\"{isoNow}\",\"N\":635476923356034756,\"@mt\":\"N{{N}}\"}}",
                HttpStatusCode.Created);
            await IngestClef(connection, "{}", HttpStatusCode.BadRequest);
            await IngestClef(connection, "{hello!}", HttpStatusCode.BadRequest);
            await IngestClef(connection, "{\"@t\":\"ceci n'est pas un timestamp\"}", HttpStatusCode.BadRequest);
            await IngestClef(connection, new string('X', 100), HttpStatusCode.BadRequest);
            await IngestClef(connection, new string('X', batchSizeLimit + 100), HttpStatusCode.RequestEntityTooLarge);
            await IngestClef(connection, $"{{\"@t\":\"{isoNow}\",\"X\":\"{new string('X', eventSizeLimit + 1000)}\"}}",
                HttpStatusCode.BadRequest);
            await IngestClef(connection, $"{{\"@t\":\"{isoNow}\",\"X\":\"{new string('X', eventSizeLimit - 1000)}\"}}",
                HttpStatusCode.Created);
            await IngestClef(connection,
                $"{{\"@t\":\"{isoNow}\",\"@tr\":\"abc\",\"@sp\":\"def\",\"@ra\":{{\"ghi\":\"jkl\"}},\"@sa\":{{\"mno\":\"pqr\"}}}}",
                HttpStatusCode.Created);
            await IngestClef(connection,
                $"{{\"@t\":\"{isoNow}\",\"@m\":\"Not a template {{foo: 'bar'}}\",\"@i\":\"01234567\"}}",
                HttpStatusCode.Created);
        }
    }

    static async Task IngestClef(SeqConnection connection, string clef, HttpStatusCode expectedStatusCode)
    {
        var content = new StringContent(clef);
        var response = await connection.Client.HttpClient.PostAsync("ingest/clef", content);
        Assert.Equal(expectedStatusCode, response.StatusCode);
    }
}
