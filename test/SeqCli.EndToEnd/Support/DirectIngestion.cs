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
        var content = new StringContent($"{prefix}{fields.Replace("'", "\"")}{suffix}");
        var response = await connection.Client.HttpClient.PostAsync("ingest/clef", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}