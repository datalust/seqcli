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
        readonly Dictionary<string, string> _idToReferenceName = new();
        readonly HashSet<PropertyInfo> _referenceProperties = new();
        readonly HashSet<PropertyInfo> _referenceListProperties = new();
        readonly Dictionary<PropertyInfo, string> _argProperties = new();

        static PropertyInfo GetProperty<T>(string propertyName) =>
            typeof(T).GetProperty(propertyName) ??
            throw new ArgumentException($"No property `{propertyName}` found on {typeof(T)}");
        
        public void MapAsReference<T>(string propertyName)
        {
            _referenceProperties.Add(GetProperty<T>(propertyName));
        }

        public void MapAsReferenceList<T>(string propertyName)
        {
            _referenceListProperties.Add(GetProperty<T>(propertyName));
        }

        public void MapNonNullAsArg<T>(string propertyName, string argumentName)
        {
            _argProperties.Add(GetProperty<T>(propertyName), argumentName);
        }
        
        public void AddReferencedTemplate(string entityId, string name)
        {
            _idToReferenceName.Add(entityId, name);
        }

        public bool TryGetRawValue(PropertyInfo pi, object? value, [MaybeNullWhen(false)] out string raw)
        {
            if (value is string s && _referenceProperties.Contains(pi) &&
                _idToReferenceName.TryGetValue(s, out var name))
            {
                raw = FormatReference(name);
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

        public bool TryGetRawElement(PropertyInfo pi, object? elementValue, [MaybeNullWhen(false)] out string raw)
        {
            if (elementValue is string s && _referenceListProperties.Contains(pi) &&
                _idToReferenceName.TryGetValue(s, out var name))
            {
                raw = FormatReference(name);
                return true;
            }

            raw = null;
            return false;
        }

        static string FormatReference(string name)
        {
            var jsonStringName = JsonConvert.SerializeObject(name);
            return $"ref({jsonStringName})";
        }
    }
}
