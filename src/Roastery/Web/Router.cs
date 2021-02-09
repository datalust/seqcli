using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;
using Serilog.Context;

namespace Roastery.Web
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

        readonly IList<(HttpMethod, Regex, RouteBinding)> _routes = new List<(HttpMethod, Regex, RouteBinding)>();
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
                    var controllerName = controller.GetType().Name;
                    var actionName = method.Name;
                    
                    var binding = new RouteBinding(
                        controllerName,
                        actionName,
                        r =>
                        {
                            using var _ = LogContext.PushProperty("Controller", controllerName);
                            using var __ = LogContext.PushProperty("Action", actionName);
                            return (Task<HttpResponse>) method.Invoke(controller, new object[] {r});
                        });
                    
                    _logger.Debug("Binding route HTTP {Method} {Path} to action method {Controller}.{Action}()",
                        route.Method, route.Path, binding.Controller, binding.Action);

                    var rx = new Regex("^" + route.Path.Replace("{id}", "[^/]+") + "$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    _routes.Add((new HttpMethod(route.Method.ToUpperInvariant()), rx, binding));
                }
            }
        }

        public override async Task<HttpResponse> InvokeAsync(HttpRequest request)
        {
            _logger.Debug("Resolving route HTTP {Method} {Path}", request.Method, request.Path);

            var requestPath = request.Path.TrimStart('/');
            var (_, _, route) = _routes.FirstOrDefault(r => r.Item1 == request.Method && r.Item2.IsMatch(requestPath));
            if (route == null)
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