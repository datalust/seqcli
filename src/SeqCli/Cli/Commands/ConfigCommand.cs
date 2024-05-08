// Copyright 2018-2021 Datalust Pty Ltd
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeqCli.Config;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands;

[Command("config", "View and set fields in the `SeqCli.json` file; run with no arguments to list all fields")]
class ConfigCommand : Command
{
    string? _key, _value;
    bool _clear;

    public ConfigCommand()
    {
        Options.Add("k|key=", "The field, for example `connection.serverUrl`", k => _key = k);
        Options.Add("v|value=", "The field value; if not specified, the command will print the current value", v => _value = v);
        Options.Add("c|clear", "Clear the field", _ => _clear = true);
    }

    protected override Task<int> Run()
    {
        var verb = "read";
            
        try
        {
            var config = SeqCliConfig.Read();

            if (_key != null)
            {
                if (_clear)
                {
                    verb = "clear";
                    Clear(config, _key);
                    SeqCliConfig.Write(config);
                }
                else if (_value != null)
                {
                    verb = "update";
                    Set(config, _key, _value);
                    SeqCliConfig.Write(config);
                }
                else
                {
                    Print(config, _key);
                }
            }
            else
            {
                List(config);
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not {Verb} config: {ErrorMessage}", verb, Presentation.FormattedMessage(ex));
            return Task.FromResult(1);
        }
    }

    static void Print(SeqCliConfig config, string key)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (key == null) throw new ArgumentNullException(nameof(key));

        var pr = ReadPairs(config).SingleOrDefault(p => p.Key == key);
        if (pr.Key == null)
            throw new ArgumentException($"Option {key} not found.");

        Console.WriteLine(Format(pr.Value));
    }

    static void Set(SeqCliConfig config, string key, string? value)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (key == null) throw new ArgumentNullException(nameof(key));

        var steps = key.Split('.');
        if (steps.Length != 2)
            throw new ArgumentException("The format of the key is incorrect; run the command without any arguments to view all keys.");

        var first = config.GetType().GetTypeInfo().DeclaredProperties
            .Where(p => p.CanRead && p.GetMethod!.IsPublic && !p.GetMethod.IsStatic)
            .SingleOrDefault(p => Camelize(p.Name) == steps[0]);

        if (first == null)
            throw new ArgumentException("The key could not be found; run the command without any arguments to view all keys.");

        if (first.PropertyType == typeof(Dictionary<string, ConnectionConfig>))
            throw new NotSupportedException("Use `seqcli profile create` to configure connection profiles.");

        var second = first.PropertyType.GetTypeInfo().DeclaredProperties
            .Where(p => p.CanRead && p.GetMethod!.IsPublic && !p.GetMethod.IsStatic)
            .SingleOrDefault(p => Camelize(p.Name) == steps[1]);

        if (second == null)
            throw new ArgumentException("The key could not be found; run the command without any arguments to view all keys.");
            
        if (!second.CanWrite || !second.SetMethod!.IsPublic)
            throw new ArgumentException("The value is not writeable.");

        var targetValue = Convert.ChangeType(value, second.PropertyType, CultureInfo.InvariantCulture);
        var configItem = first.GetValue(config);
        second.SetValue(configItem, targetValue);
    }

    static void Clear(SeqCliConfig config, string key)
    {
        Set(config, key, null);
    }

    static void List(SeqCliConfig config)
    {
        foreach (var (key, value) in ReadPairs(config))
        {
            Console.WriteLine($"{key}:");
            Console.WriteLine($"  {Format(value)}");
        }
    }

    static IEnumerable<KeyValuePair<string, object?>> ReadPairs(object config)
    {
        foreach (var property in config.GetType().GetTypeInfo().DeclaredProperties
                     .Select(p => new { Property = p, Name = GetConfigPropertyName(p)})
                     .Where(p => p.Property.CanRead && p.Property.GetMethod!.IsPublic && !p.Property.GetMethod.IsStatic && !p.Name.StartsWith("encoded", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(p => p.Name))
        {
            var propertyName = Camelize(property.Name);
            var propertyValue = property.Property.GetValue(config);

            if (propertyValue is IDictionary dict)
            {
                foreach (var elementKey in dict.Keys)
                {
                    foreach (var elementPair in ReadPairs(dict[elementKey]!))
                    {
                        yield return new KeyValuePair<string, object?>(
                            $"{propertyName}[{elementKey}].{elementPair.Key}",
                            elementPair.Value);
                    }
                }
            }
            else if (propertyValue?.GetType().Namespace?.StartsWith("SeqCli.Config") ?? false)
            {
                foreach (var childPair in ReadPairs(propertyValue))
                {
                    var name = propertyName + "." + childPair.Key;
                    yield return new KeyValuePair<string, object?>(name, childPair.Value);
                }
            }
            else
            {
                yield return new KeyValuePair<string, object?>(propertyName, propertyValue);
            }
        }
    }

    static string GetConfigPropertyName(PropertyInfo property)
    {
        return property.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? property.Name;
    }

    static string Camelize(string s)
    {
        if (s.Length < 2)
            throw new NotSupportedException("No camel-case support for short names");
        return char.ToLowerInvariant(s[0]) + s[1..];
    }

    static string Format(object? value)
    {
        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value?.ToString() ?? "";
    }
}