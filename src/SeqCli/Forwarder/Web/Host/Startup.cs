using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Seq.Forwarder.Web.Host
{
    class Startup
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (RequestProcessingException rex)
                {
                    if (context.Response.HasStarted)
                        throw;

                    context.Response.StatusCode = (int)rex.StatusCode;
                    context.Response.ContentType = "text/plain; charset=UTF-8";
                    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(rex.Message));
                    await context.Response.CompleteAsync();
                }
            });
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}