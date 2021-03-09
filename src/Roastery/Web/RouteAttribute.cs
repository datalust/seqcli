using System;

namespace Roastery.Web
{
    [AttributeUsage(AttributeTargets.Method)]
    class RouteAttribute : Attribute
    {
        public string Method { get; }
        public string Path { get; }

        public RouteAttribute(string method, string path)
        {
            Method = method;
            Path = path;
        }
    }
}