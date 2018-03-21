using System;
using System.Net;
using System.Threading.Tasks;
using Seq.Api;

namespace SeqCli.EndToEnd.Support
{
    static class SeqConnectionExtensions
    {
        public static async Task EnsureConnected(this SeqConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var started = DateTime.UtcNow;
            var wait = TimeSpan.FromMilliseconds(100);
            var waitLimit = TimeSpan.FromSeconds(30);
            var deadline = started.Add(waitLimit);
            while (!await ConnectAsync(connection, DateTime.UtcNow > deadline))
            {
                await Task.Delay(wait);
            }
        }

        static async Task<bool> ConnectAsync(SeqConnection connection, bool throwOnFailure)
        {
            HttpStatusCode statusCode;

            try
            {
                statusCode = (await connection.Client.HttpClient.GetAsync("/api")).StatusCode;
            }
            catch
            {
                if (throwOnFailure)
                    throw;
                
                return false;
            }

            if (statusCode == HttpStatusCode.OK)
                return true;

            if (!throwOnFailure)
                return false;

            throw new Exception($"Could not connect to the Seq API endpoint: {statusCode}.");
        }
    }
}