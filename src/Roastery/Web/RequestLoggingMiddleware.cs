using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Threading.Tasks;
using Roastery.Metrics;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using SerilogTracing;

namespace Roastery.Web;

class RequestLoggingMiddleware : HttpServer
{
    readonly HttpServer _next;
    readonly ILogger _logger;
    readonly RoasteryWebMetrics _metrics;

    public RequestLoggingMiddleware(ILogger logger, RoasteryWebMetrics metrics, HttpServer next)
    {
        _next = next;
        _logger = logger.ForContext<RequestLoggingMiddleware>();
        _metrics = metrics;
    }

    public override async Task<HttpResponse> InvokeAsync(HttpRequest request)
    {
        using var _ = LogContext.PushProperty("RequestId", request.RequestId);

        var requestTiming = Stopwatch.StartNew();
        using var activity = _logger.StartActivity("HTTP {RequestMethod} {RequestPath}", request.Method, request.Path);

        try
        {
            var response = await _next.InvokeAsync(request);

            LogCompletion(activity, null, response.StatusCode);
            _metrics.RecordHttpRequestDuration(
                new RoasteryWebMetrics.Sample.HttpRequestDurationKey(request.Path, (int)response.StatusCode),
                requestTiming.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex1) when (LogCompletion(activity, ex1, HttpStatusCode.InternalServerError))
        {
            // We never hit this, because the exception filter always returns false.
            throw;
        }
        catch
        {
            var statusCode = HttpStatusCode.InternalServerError;

            _metrics.RecordHttpRequestDuration(
                new RoasteryWebMetrics.Sample.HttpRequestDurationKey(request.Path, (int)statusCode),
                requestTiming.ElapsedMilliseconds);
            return new HttpResponse(statusCode, "An error occurred.");
        }
    }

    bool LogCompletion(LoggerActivity activity, Exception? exception, HttpStatusCode statusCode)
    {
        var level = (int)statusCode >= 500 ? LogEventLevel.Error : LogEventLevel.Information;

        activity.AddProperty("StatusCode", (int)statusCode);
        activity.Complete(level, exception);

        return false;
    }
}