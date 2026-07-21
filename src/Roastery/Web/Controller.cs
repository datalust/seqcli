using System.Net;
using Roastery.Metrics;
using Serilog;

namespace Roastery.Web;

abstract class Controller
{
    protected ILogger Log { get; }
    protected RoasteryWebMetrics Metrics { get; }
    
    protected Controller(ILogger logger, RoasteryWebMetrics metrics)
    {
        Log = logger.ForContext(GetType());
        Metrics = metrics;
    }

    protected static HttpResponse Json(object? body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new(statusCode, body);
    }

    protected HttpResponse BadRequest(string? reason = null)
    {
        Log.Debug("Bad request: {Reason}", reason);
        return new HttpResponse(HttpStatusCode.BadRequest, reason);
    }

    protected HttpResponse NotFound()
    {
        return new HttpResponse(HttpStatusCode.NotFound, "The resource was not found on this server.");
    }

    protected HttpResponse Conflict(string? reason = null)
    {
        Log.Debug("Conflict: {Reason}", reason);
        return new HttpResponse(HttpStatusCode.Conflict, reason);
    }

    protected HttpResponse OK()
    {
        return new HttpResponse(HttpStatusCode.OK, null);
    }
}