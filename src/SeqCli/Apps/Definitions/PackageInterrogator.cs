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

using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SeqCli.Apps.Hosting;

namespace SeqCli.Apps.Definitions
{
    static class PackageInterrogator
    {
        public static string FindAppConfiguration(string path, string mainAppTypeName, bool formatIndented)
        {
            using var appLoader = new AppLoader(path);
            if (appLoader.TryLoadSeqAppType(mainAppTypeName, out var reactorType))
            {
                var json = new StringWriter();
                var serializer = JsonSerializer.Create(
                    new JsonSerializerSettings
                    {
                        Formatting = formatIndented ? Formatting.Indented : Formatting.None,
                        ContractResolver = new CamelCasePropertyNamesContractResolver
                        {
                            NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false }
                        },
                        Converters =
                        {
                            new StringEnumConverter(new CamelCaseNamingStrategy())
                        }
                    });
                
                serializer.Serialize(json, AppMetadataReader.ReadFromSeqAppType(reactorType));
                return json.ToString();
            }
            return null;
        }
    }
}
