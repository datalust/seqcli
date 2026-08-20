using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Seq.Api;
using Xunit;

namespace SeqCli.EndToEnd.Support;

public static class DirectIngestion
{
    // In questionable taste, but very handy, `fields` carries the comma-separated `'key': value` pairs massaged
    // into JSON by replacing `'` with `"`.
    public static async Task IngestClef(SeqConnection connection, string fields)
    {
        var prefix = $"{{\"@t\":\"{DateTime.UtcNow:o}\",";
        const string suffix = "}";
        await IngestClefDocument(connection, $"{prefix}{fields.Replace("'", "\"")}{suffix}");
    }

    public static async Task IngestClefDocument(SeqConnection connection, string clef)
    {
        var response = await connection.Client.HttpClient.PostAsync("ingest/clef", new StringContent(clef));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}