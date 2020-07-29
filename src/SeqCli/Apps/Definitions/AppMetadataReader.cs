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
using System.Linq;
using System.Reflection;
using Seq.Apps;

namespace SeqCli.Apps.Definitions
{
    class AppMetadataReader
    {
        public static AppDefinition ReadFromReactorType(Type mainReactorType)
        {
            if (mainReactorType == null) throw new ArgumentNullException(nameof(mainReactorType));

            var declared = mainReactorType.GetCustomAttribute<SeqAppAttribute>();
            if (declared == null)
                throw new ArgumentException($"The provided type '{mainReactorType}' is not marked with [SeqApp].");

            var app = new AppDefinition
            {
                Name = declared.Name,
                __MainReactorTypeName = mainReactorType.FullName,
                Description = declared.Description,
                AllowReprocessing = declared.AllowReprocessing,
                Settings = GetAvailableSettings(mainReactorType),
                Capabilities = GetCapabilities(mainReactorType)
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
                        DisplayName = p.attr.DisplayName,
                        IsOptional = p.attr.IsOptional,
                        HelpText = p.attr.HelpText,
                        InputType = p.attr.InputType == SettingInputType.Unspecified ?
                            GetSettingType(p.pi.PropertyType) :
                            (AppSettingType)Enum.Parse(typeof(AppSettingType), p.attr.InputType.ToString()),
                        IsInvocationParameter = p.attr.IsInvocationParameter
                    });
        }

        static readonly HashSet<Type> IntegerTypes = new HashSet<Type>
        {
            typeof(short), typeof(short?), typeof(ushort), typeof(ushort?),
            typeof(int), typeof(int?), typeof(uint), typeof(uint?),
            typeof(long), typeof(long?), typeof(ulong), typeof(ulong?)
        };

        static readonly HashSet<Type> DecimalTypes = new HashSet<Type>
        {
            typeof(float), typeof(double), typeof(decimal),
            typeof(float?), typeof(double?), typeof(decimal?)
        };

        static readonly HashSet<Type> BooleanTypes = new HashSet<Type>
        {
            typeof(bool), typeof(bool?)
        };

        static AppSettingType GetSettingType(Type type)
        {
            if (IntegerTypes.Contains(type))
                return AppSettingType.Integer;

            if (DecimalTypes.Contains(type))
                return AppSettingType.Decimal;

            if (BooleanTypes.Contains(type))
                return AppSettingType.Checkbox;

            return AppSettingType.Text;
        }
    }
}
