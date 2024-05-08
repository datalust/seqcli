using Microsoft.AspNetCore.Builder;

namespace SeqCli.Forwarder.Web.Api;

interface IMapEndpoints
{
    void MapEndpoints(WebApplication app);
}