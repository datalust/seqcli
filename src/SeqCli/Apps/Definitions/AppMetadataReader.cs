// Copyright 2020 Datalust Pty Ltd and Contributors
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Seq.Apps;

namespace SeqCli.Apps.Definitions;

static class AppMetadataReader
{
    public static AppDefinition ReadFromSeqAppType(Type seqAppType)
    {
        if (seqAppType == null) throw new ArgumentNullException(nameof(seqAppType));

        var declared = seqAppType.GetCustomAttribute<SeqAppAttribute>();
        if (declared == null)
            throw new ArgumentException($"The provided type '{seqAppType}' is not marked with [SeqApp].");

        var app = new AppDefinition(declared.Name)
        {
            Description = declared.Description,
            AllowReprocessing = declared.AllowReprocessing,
            Settings = GetAvailableSettings(seqAppType),
            Capabilities = GetCapabilities(seqAppType),
            Platform = new Dictionary<string, AppPlatformDefinition>
            {
                ["hosted-dotnet"] = new()
                {
                    SeqAppTypeName = seqAppType.FullName
                }
            }
        };

        return app;
    }

    static List<string> GetCapabilities(Type mainReactorType)
    {
        var caps = new List<string>();
        if (typeof(IPublishJson).IsAssignableFrom(mainReactorType))
            caps.Add("input");
        return caps;
    }

    static Dictionary<string, AppSettingDefinition> GetAvailableSettings(Type mainReactorType)
    {
        var properties = mainReactorType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties
            .Select(pi => new { pi, attr = pi.GetCustomAttribute<SeqAppSettingAttribute>() })
            .Where(p => p.attr != null)
            .ToDictionary(
                p => p.pi.Name,
                p => new AppSettingDefinition
                {
                    DisplayName = Normalize(p.attr!.DisplayName),
                    IsOptional = p.attr.IsOptional,
                    HelpText = Normalize(p.attr.HelpText),
                    InputType = p.attr.InputType == SettingInputType.Unspecified ?
                        GetSettingType(p.pi.PropertyType) :
                        (AppSettingType)Enum.Parse(typeof(AppSettingType), p.attr.InputType.ToString()),
                    IsInvocationParameter = p.attr.IsInvocationParameter,
                    AllowedValues = TryGetAllowedValues(p.pi.PropertyType),
                    Syntax = Normalize(p.attr.Syntax)
                });
    }

    static readonly HashSet<Type> IntegerTypes = new()
    {
        typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong)
    };

    static readonly HashSet<Type> DecimalTypes = new()
    {
        typeof(float), typeof(double), typeof(decimal)
    };

    static readonly HashSet<Type> BooleanTypes = new()
    {
        typeof(bool)
    };

    internal static AppSettingType GetSettingType(Type type)
    {
        var targetType = Nullable.GetUnderlyingType(type) ?? type;
            
        if (IntegerTypes.Contains(targetType))
            return AppSettingType.Integer;

        if (DecimalTypes.Contains(targetType))
            return AppSettingType.Decimal;

        if (BooleanTypes.Contains(targetType))
            return AppSettingType.Checkbox;

        if (targetType.IsEnum)
            return AppSettingType.Select;

        return AppSettingType.Text;
    }

    internal static AppSettingValue[]? TryGetAllowedValues(Type type)
    {
        var targetType = Nullable.GetUnderlyingType(type) ?? type;
            
        if (!targetType.IsEnum)
            return null;

        // Preserve declaration order
        var values =
            from name in Enum.GetNames(targetType)
            let member = targetType.GetField(name)
            let description = member?.GetCustomAttribute<DescriptionAttribute>()?.Description
            select new AppSettingValue {Value = name, Description = description};

        return values.ToArray();
    }

    static string? Normalize(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}