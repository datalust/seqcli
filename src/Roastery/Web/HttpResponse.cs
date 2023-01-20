using System.Net;
using System.Net.Http;

namespace Roastery.Web;

class HttpResponse
{
    public HttpStatusCode StatusCode { get; }
    public object? Body { get; }

    public HttpResponse(HttpStatusCode statusCode, object? body = null)
    {
        StatusCode = statusCode;
        Body = body;
    }

    public void EnsureSuccessStatusCode()
    {
        if ((int) StatusCode >= 400)
            throw new HttpRequestException($"Request failed with status code {(int) StatusCode}/{StatusCode}.");
    }
}