using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json;

#nullable enable

namespace SeqCli.Templates.Export
{
    class TemplateValueMap
    {
        readonly Dictionary<string, string> _idToFilename = new();
        readonly HashSet<PropertyInfo> _referenceProperties = new();
        readonly Dictionary<PropertyInfo, string> _argProperties = new();

        static PropertyInfo GetProperty<T>(string propertyName) =>
            typeof(T).GetProperty(propertyName) ??
            throw new ArgumentException($"No property `{propertyName}` found on {typeof(T)}");
        
        public void MapAsReference<T>(string propertyName)
        {
            _referenceProperties.Add(GetProperty<T>(propertyName));
        }

        public void MapNonNullAsArg<T>(string propertyName, string argumentName)
        {
            _argProperties.Add(GetProperty<T>(propertyName), argumentName);
        }
        
        public void AddReferencedTemplate(string entityId, string filename)
        {
            _idToFilename.Add(entityId, filename);
        }

        public bool TryGetRawValue(PropertyInfo pi, object? value, [MaybeNullWhen(false)] out string raw)
        {
            if (value is string s && _referenceProperties.Contains(pi) &&
                _idToFilename.TryGetValue(s, out var filename))
            {
                var jsonStringFilename = JsonConvert.SerializeObject(filename);
                raw = $"ref({jsonStringFilename})";
                return true;
            }

            if (value != null && _argProperties.TryGetValue(pi, out var arg))
            {
                var jsonStringArg = JsonConvert.SerializeObject(arg);
                raw = $"arg({jsonStringArg})";
                return true;
            }

            raw = null;
            return false;
        }
    }
}