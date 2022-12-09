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
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SeqCli.Apps.Definitions;

static class AppDefinitionFormatter
{
    public static void FormatAppDefinition(Type seqAppType, bool formatIndented, TextWriter output)
    {
        if (seqAppType == null) throw new ArgumentNullException(nameof(seqAppType));
        if (output == null) throw new ArgumentNullException(nameof(output));

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
            
        serializer.Serialize(output, AppMetadataReader.ReadFromSeqAppType(seqAppType));
    }
}