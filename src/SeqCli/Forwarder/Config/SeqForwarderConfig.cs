// Copyright 2016-2017 Datalust Pty Ltd
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
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Seq.Forwarder.Config
{
    class SeqForwarderConfig
    {
        static JsonSerializerSettings SerializerSettings { get; } = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters =
            {
                new StringEnumConverter()
            }
        };

        public static SeqForwarderConfig ReadOrInit(string filename, bool includeEnvironmentVariables = true)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            if (!File.Exists(filename))
            {
                var config = new SeqForwarderConfig();
                Write(filename, config);
                return config;
            }

            var content = File.ReadAllText(filename);
            var combinedConfig = JsonConvert.DeserializeObject<SeqForwarderConfig>(content, SerializerSettings)
                ?? throw new ArgumentException("Configuration content is null.");

            if (includeEnvironmentVariables)
            {
                // Any Environment Variables overwrite those in the Config File
                var envVarConfig = new ConfigurationBuilder().AddEnvironmentVariables("FORWARDER_").Build();
                foreach (var sectionProperty in typeof(SeqForwarderConfig).GetTypeInfo().DeclaredProperties
                    .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && !p.GetMethod.IsStatic))
                {
                    foreach (var subGroupProperty in sectionProperty.PropertyType.GetTypeInfo().DeclaredProperties
                        .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && p.SetMethod != null && p.SetMethod.IsPublic && !p.GetMethod.IsStatic))
                    {
                        var envVarName = sectionProperty.Name.ToUpper() + "_" + subGroupProperty.Name.ToUpper();
                        var envVarVal = envVarConfig.GetValue(subGroupProperty.PropertyType, envVarName);
                        if (envVarVal != null)
                        {
                            subGroupProperty.SetValue(sectionProperty.GetValue(combinedConfig), envVarVal);
                        }
                    }
                }
            }

            return combinedConfig;
        }

        public static void Write(string filename, SeqForwarderConfig data)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (data == null) throw new ArgumentNullException(nameof(data));

            var dir = Path.GetDirectoryName(filename);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);
            
            var content = JsonConvert.SerializeObject(data, Formatting.Indented, SerializerSettings);
            File.WriteAllText(filename, content);
        }

        public SeqForwarderDiagnosticConfig Diagnostics { get; set; } = new SeqForwarderDiagnosticConfig();
        public SeqForwarderOutputConfig Output { get; set; } = new SeqForwarderOutputConfig();
        public SeqForwarderStorageConfig Storage { get; set; } = new SeqForwarderStorageConfig();
        public SeqForwarderApiConfig Api { get; set; } = new SeqForwarderApiConfig();
    }
}
