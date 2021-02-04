using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;

namespace SeqCli.Sample.Loader.Web
{
    class Router : HttpServer
    {
        delegate Task<HttpResponse> ActionMethod(HttpRequest request);

        class RouteBinding
        {
            public string Controller { get; }
            public string Action { get; }
            public ActionMethod Binding { get; }

            public RouteBinding(string controller, string action, ActionMethod binding)
            {
                Controller = controller;
                Action = action;
                Binding = binding;
            }
        }

        readonly IDictionary<(HttpMethod, string), RouteBinding> _routes = new Dictionary<(HttpMethod, string), RouteBinding>();
        readonly ILogger _logger;

        public Router(IEnumerable<Controller> controllers, ILogger logger)
        {
            _logger = logger.ForContext<Router>();
            
            _logger.Debug("Building route table from controller metadata");

            foreach (var controller in controllers)
            {
                var actionMethods = controller.GetType().GetTypeInfo().DeclaredMethods
                    .Select(m => (method: m, route: m.GetCustomAttribute<RouteAttribute>()))
                    .Where(mr => mr.route != null);

                foreach (var (method, route) in actionMethods)
                {
                    var binding = new RouteBinding(
                        controller.GetType().Name,
                        method.Name,
                        r => (Task<HttpResponse>) method.Invoke(controller, new object[] {r}));
                    
                    _logger.Debug("Binding route HTTP {Method} {Path} to action method {Controller}.{Action}()",
                        route.Method, route.Path, binding.Controller, binding.Action);
                    
                    _routes.Add((new HttpMethod(route.Method.ToUpperInvariant()), route.Path), binding);
                }
            }
        }

        public override async Task<HttpResponse> InvokeAsync(HttpRequest request)
        {
            _logger.Debug("Resolving route HTTP {Method} {Path}", request.Method, request.Path);

            if (!_routes.TryGetValue((request.Method, request.Path.TrimStart('/')), out var route))
            {
                _logger.Debug("No action method is bound for this route");
                return new HttpResponse(HttpStatusCode.NotFound,
                    $"The resource {request.Path} was not found on this server.");
            }

            _logger.Debug("Invoking action method {Controller}.{Action}()", route.Controller, route.Action);
            return await route.Binding(request);
        }
    }
}