using System;
using System.Net;
using System.Threading.Tasks;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using SerilogTracing;
using SerilogTracing.Instrumentation;

namespace Roastery.Web;

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

        using var activity = _logger.StartActivity("HTTP {RequestMethod} {RequestPath}", request.Method, request.Path);

        try
        {
            var response = await _next.InvokeAsync(request);
            LogCompletion(activity, null, response.StatusCode);
            return response;
        }
        catch (Exception ex1) when (LogCompletion(activity, ex1, HttpStatusCode.InternalServerError))
        {
            // We never hit this, because the exception filter always returns false.
            throw;
        }
        catch
        {
            return new HttpResponse(HttpStatusCode.InternalServerError, "An error occurred.");
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