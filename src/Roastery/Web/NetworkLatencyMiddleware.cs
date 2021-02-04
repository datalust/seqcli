using System;
using System.Threading.Tasks;

namespace Roastery.Web
{
    class NetworkLatencyMiddleware : HttpServer
    {
        readonly HttpServer _next;
        readonly Random _rng = new Random(DateTime.Now.Millisecond);

        public NetworkLatencyMiddleware(HttpServer next)
        {
            _next = next;
        }

        public override async Task<HttpResponse> InvokeAsync(HttpRequest request)
        {
            await Task.Delay(100 + (int)(_rng.NextDouble() * 300));
            var response = await _next.InvokeAsync(request);
            await Task.Delay(10 + (int) (_rng.NextDouble() * 100));
            return response;
        }
    }
}