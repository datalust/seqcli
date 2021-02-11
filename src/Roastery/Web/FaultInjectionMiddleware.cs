using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Util;
using Serilog;

namespace Roastery.Web
{
    class FaultInjectionMiddleware: HttpServer
    {
        readonly ILogger _logger;
        readonly HttpServer _next;
        readonly Func<HttpRequest, Task<HttpResponse>>[] _faults;

        public FaultInjectionMiddleware(ILogger logger, HttpServer next)
        {
            _logger = logger.ForContext<FaultInjectionMiddleware>();
            _next = next;
            _faults = new Func<HttpRequest, Task<HttpResponse>>[]
            {
                Unauthorized,
                Unauthorized,
                Unauthorized,
                Timeout,
                Timeout,
                Disposed
            };
        }

        Task<HttpResponse> Unauthorized(HttpRequest request)
        {
            _logger.Debug("Could not validate authentication token: token is expired");
            return Task.FromResult(new HttpResponse(HttpStatusCode.Unauthorized, "Please log in."));
        }

        static async Task<HttpResponse> Timeout(HttpRequest request)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);
            await Task.Delay(-1, cts.Token);
            throw new InvalidOperationException("Should never reach this.");
        }
        
        static Task<HttpResponse> Disposed(HttpRequest request)
        {
            throw new ObjectDisposedException("TcpConnection");
        }

        static Task<HttpResponse> Dropped()
        {
            throw new IOException("An operation was attempted on a nonexistent network connection.");
        }
        
        public override async Task<HttpResponse> InvokeAsync(HttpRequest request)
        {
            if (Distribution.OnceIn(220))
            {
                var fault = Distribution.Uniform(_faults);
                return await fault(request);
            }

            var result = await _next.InvokeAsync(request);

            if (Distribution.OnceIn(280))
            {
                return await Dropped();
            }

            return result;
        }
    }
}