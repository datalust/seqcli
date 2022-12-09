using System.Net.Http;
using System.Threading.Tasks;

namespace Roastery.Web;

class HttpClient
{
    readonly string _basePath;
    readonly HttpServer _server;

    public HttpClient(string basePath, HttpServer server)
    {
        _basePath = basePath.TrimEnd('/') + '/';
        _server = server;
    }

    public async Task<TResponse> GetAsync<TResponse>(string path)
    {
        var request = new HttpRequest(HttpMethod.Get, _basePath + path);
        var response = await _server.InvokeAsync(request);
        response.EnsureSuccessStatusCode();
        return (TResponse)response.Body!;
    }

    public async Task PutAsync(string path, object body)
    {
        var request = new HttpRequest(HttpMethod.Put, _basePath + path, body);
        var response = await _server.InvokeAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TResponse> PostAsync<TResponse>(string path, object body)
    {
        var request = new HttpRequest(HttpMethod.Post, _basePath + path, body);
        var response = await _server.InvokeAsync(request);
        response.EnsureSuccessStatusCode();
        return (TResponse) response.Body!;
    }

    public async Task DeleteAsync(string path)
    {
        var request = new HttpRequest(HttpMethod.Delete, _basePath + path);
        var response = await _server.InvokeAsync(request);
        response.EnsureSuccessStatusCode();
    }
}