using System;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Util;

namespace Roastery.Web
{
    class SchedulingLatencyMiddleware : HttpServer
    {
        readonly HttpServer _next;

        const int Capacity = 16; // 16 concurrent requests is just fine :-)
        int _activeRequests;

        public SchedulingLatencyMiddleware(HttpServer next)
        {
            _next = next;
        }

        public override async Task<HttpResponse> InvokeAsync(HttpRequest request)
        {
            var current = Interlocked.Increment(ref _activeRequests);
            try
            {
                var delay = (int)(10 * Distribution.Uniform());
                if (current > Capacity)
                {
                    // One extra millisecond per concurrent request over capacity, ramping up
                    delay += (int) Math.Pow(current - Capacity, 1.6);
                }
                await Task.Delay(delay);
                return await _next.InvokeAsync(request);
            }
            finally
            {
                Interlocked.Decrement(ref _activeRequests);
            }
        }
    }
}