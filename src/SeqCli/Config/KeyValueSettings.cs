// Copyright © Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace SeqCli.Config;

static class KeyValueSettings
{
    public static void Set(SeqCliConfig config, string key, string? value)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (key == null) throw new ArgumentNullException(nameof(key));

        var steps = key.Split('.');
        if (steps.Length < 2)
            throw new ArgumentException("The format of the key is incorrect; run `seqcli config list` to view all keys.");

        object? receiver = config;
        for (var i = 0; i < steps.Length - 1; ++i)
        {
            var nextStep = receiver.GetType().GetTypeInfo().DeclaredProperties
                .Where(p => p.CanRead && p.GetMethod!.IsPublic && !p.GetMethod.IsStatic && p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .SingleOrDefault(p => Camelize(GetUserFacingName(p)) == steps[i]);

            if (nextStep == null)
                throw new ArgumentException("The key could not be found; run `seqcli config list` to view all keys.");

            if (nextStep.PropertyType == typeof(Dictionary<string, SeqCliConnectionConfig>))
                throw new NotSupportedException("Use `seqcli profile create` to configure connection profiles.");
            
            receiver = nextStep.GetValue(receiver);
            if (receiver == null)
                throw new InvalidOperationException("Intermediate configuration object is null.");
        }

        // FUTURE: the use of `p.Name` and lack of `JsonIgnoreAttribute` checks here mean that sensitive values can
        // intercept writes through hidden properties, triggering encoding where supported. A type-based solution
        // would be more robust.
        var targetProperty = receiver.GetType().GetTypeInfo().DeclaredProperties
            .Where(p => p is { CanRead: true, CanWrite: true } && p.GetMethod!.IsPublic && p.SetMethod!.IsPublic && !p.GetMethod.IsStatic)
            .SingleOrDefault(p => Camelize(GetUserFacingName(p)) == steps[^1]);

        if (targetProperty == null)
            throw new ArgumentException("The key could not be found; run `seqcli config list` to view all keys.");

        var targetValue = ChangeType(value, targetProperty.PropertyType);
        targetProperty.SetValue(receiver, targetValue);
    }

    static object? ChangeType(string? value, Type propertyType)
    {
        if (propertyType == typeof(string[]))
            return value?.Split(',').Select(e => e.Trim()).ToArray() ?? [];

        if (propertyType == typeof(int[]))
            return value?.Split(',').Select(e => int.Parse(e.Trim(), CultureInfo.InvariantCulture)).ToArray() ?? [];

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return string.IsNullOrWhiteSpace(value) ? null : ChangeType(value, propertyType.GetGenericArguments().Single());
        }

        if (propertyType.IsEnum)
            return Enum.Parse(propertyType, value ?? throw new ArgumentException("The setting format is incorrect."));

        return Convert.ChangeType(value, propertyType, CultureInfo.InvariantCulture);
    }

    public static void Clear(SeqCliConfig config, string key)
    {
        Set(config, key, null);
    }

    public static bool TryGetValue(object config, string key, out string? value, [NotNullWhen(true)] out PropertyInfo? metadata)
    {
        var (readKey, readValue, m) = Inspect(config).SingleOrDefault(p => p.Item1 == key);
        if (readKey == null)
        {
            value = null;
            metadata = null;
            return false;
        }

        value = readValue;
        metadata = m;
        return true;
    }
    
    public static IEnumerable<(string, string, PropertyInfo)> Inspect(object config)
    {
        return Inspect(config, null);
    }

    static IEnumerable<(string, string, PropertyInfo)> Inspect(object receiver, string? receiverName)
    {
        foreach (var nextStep in receiver.GetType().GetTypeInfo().DeclaredProperties
            .Where(p => p.CanRead && p.GetMethod!.IsPublic && 
                        !p.GetMethod.IsStatic && p.GetCustomAttribute<ObsoleteAttribute>() == null && p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            .OrderBy(GetUserFacingName))
        {
            var camel = Camelize(GetUserFacingName(nextStep));
            var valuePath = receiverName == null ? camel : $"{receiverName}.{camel}";

            if (nextStep.PropertyType.IsAssignableTo(typeof(IDictionary)))
            {
                var dict = (IDictionary)nextStep.GetValue(receiver)!;
                foreach (var elementKey in dict.Keys)
                {
                    foreach (var elementPair in Inspect(dict[elementKey]!))
                    {
                        yield return (
                            $"{valuePath}[{elementKey}].{elementPair.Item1}",
                            elementPair.Item2,
                            elementPair.Item3);
                    }
                }
            }
            // I.e. all of our nested config types
            else if (nextStep.PropertyType.Name.StartsWith("SeqCli", StringComparison.Ordinal))
            {
                var subConfig = nextStep.GetValue(receiver);
                if (subConfig != null)
                {
                    foreach (var keyValuePair in Inspect(subConfig, valuePath))
                        yield return keyValuePair;
                }
            }
            else if (nextStep.CanRead && nextStep.GetMethod!.IsPublic && 
                     nextStep.CanWrite && nextStep.SetMethod!.IsPublic && 
                     !nextStep.SetMethod.IsStatic &&
                     nextStep.GetCustomAttribute<ObsoleteAttribute>() == null &&
                     nextStep.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            {
                var value = nextStep.GetValue(receiver);
                yield return (valuePath, FormatConfigValue(value), nextStep);
            }
        }
    }

    static string FormatConfigValue(object? value)
    {
        if (value is string[] strings)
            return string.Join(",", strings);

        if (value is int[] ints)
            return string.Join(",", ints.Select(i => i.ToString(CultureInfo.InvariantCulture)));

        if (value is decimal dec)
        {
            var floor = decimal.Floor(dec);
            if (dec == floor)
                value = floor; // JSON.NET adds a trailing zero, which System.Decimal preserves
        }

        return value is IFormattable formattable ?
                formattable.ToString(null, CultureInfo.InvariantCulture) :
                value?.ToString() ?? "";
    }

    static string Camelize(string s)
    {
        if (s.Length < 2)
            throw new NotImplementedException("No camel-case support for short names");

        if (s.StartsWith("MS", StringComparison.Ordinal))
            return "ms" + s[2..];
        
        return char.ToLowerInvariant(s[0]) + s[1..];
    }
    
    static string GetUserFacingName(PropertyInfo pi)
    {
        return pi.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? pi.Name;
    }
}