using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Roastery.Web
{
    class HttpRequest
    {
        public string RequestId { get; } = Guid.NewGuid().ToString("n").Substring(10);

        public HttpMethod Method { get; }
        public string RawUrl { get; }
        public object Body { get; }
        public string Scheme { get; }
        public string Query { get; }
        public string Path { get; }
        public int Port { get; }
        public string Host { get; }

        public IDictionary<string, IEnumerable<string>> Headers { get; } =
            new Dictionary<string, IEnumerable<string>>();

        public HttpRequest(HttpMethod method, string url, object body = null)
        {
            Method = method;
            RawUrl = url;
            Body = body;
            var parts = new Uri(url);
            Scheme = parts.Scheme;
            Host = parts.Host;
            Port = parts.Port;
            Path = parts.AbsolutePath;
            Query = parts.Query;
        }
    }
}