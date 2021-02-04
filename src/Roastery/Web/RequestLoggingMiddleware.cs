using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace Roastery.Web
{
    class RequestLoggingMiddleware : HttpServer
    {
        readonly HttpServer _next;
        readonly ILogger _logger;

        public RequestLoggingMiddleware(ILogger logger, HttpServer next)
        {
            _next = next;
            _logger = logger.ForContext<RequestLoggingMiddleware>();
        }

        public override async Task<HttpResponse> InvokeAsync(HttpRequest request)
        {
            using var _ = LogContext.PushProperty("RequestId", request.RequestId);
            
            var sw = Stopwatch.StartNew();
            try
            {
                var response = await _next.InvokeAsync(request);
                LogCompletion(null, request, sw, response.StatusCode);
                return response;
            }
            catch (Exception ex1) when (LogCompletion(ex1, request, sw, HttpStatusCode.InternalServerError))
            {
                // We never hit this, because the exception filter always returns null.
                throw;
            }
            catch
            {
                return new HttpResponse(HttpStatusCode.InternalServerError, "An error occurred.");
            }
        }

        bool LogCompletion(Exception exception, HttpRequest request, Stopwatch sw, HttpStatusCode statusCode)
        {
            var level = (int)statusCode >= 500 ? LogEventLevel.Error : LogEventLevel.Information;
            _logger.Write(level, exception,
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000} ms",
                request.Method, request.Path, (int)statusCode, sw.Elapsed.TotalMilliseconds);
            return false;
        }
    }
}