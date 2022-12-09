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
using System.Globalization;
using System.Linq;
using System.Reflection;
using Seq.Apps;

namespace SeqCli.Apps.Hosting
{
    static class AppActivator
    {
        public static SeqApp CreateInstance(Type seqAppType, string title, IReadOnlyDictionary<string, string> settings)
        {
            if (seqAppType == null) throw new ArgumentNullException(nameof(seqAppType));
            if (title == null) throw new ArgumentNullException(nameof(title));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var instance = (SeqApp?)Activator.CreateInstance(seqAppType) ??
                           throw new InvalidOperationException($"The Seq app type {seqAppType} cannot be constructed.");

            var appSettings = seqAppType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(pi => new { pi, attr = pi.GetCustomAttribute<SeqAppSettingAttribute>() })
                .Where(p => p.attr != null);

            foreach (var setting in appSettings)
            {
                if (settings.TryGetValue(setting.pi.Name, out var value) &&
                    !string.IsNullOrEmpty(value))
                {
                    var converted = ConvertToSettingType(value, setting.pi.PropertyType);
                    setting.pi.SetValue(instance, converted);
                }
                else if (!setting.attr!.IsOptional)
                {
                    throw new SeqAppException(
                        $"The required setting `{setting.pi.Name}` has not been provided to {title}.");
                }
            }

            return instance;
        }

        internal static object ConvertToSettingType(string value, Type settingType)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (settingType == null) throw new ArgumentNullException(nameof(settingType));

            var targetType = Nullable.GetUnderlyingType(settingType) ?? settingType;
            if (targetType.IsEnum )
            {
                return Enum.Parse(targetType, value);
            }
            
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }
}
