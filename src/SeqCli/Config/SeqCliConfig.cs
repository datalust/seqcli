// Copyright 2018 Datalust Pty Ltd
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
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SeqCli.Config;

class SeqCliConfig
{
    bool _exportable = true;
        
    static JsonSerializerSettings SerializerSettings { get; } = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters =
        {
            new StringEnumConverter()
        }
    };

    public static SeqCliConfig ReadFromFile(string filename)
    {
        var content = File.ReadAllText(filename);
        return JsonConvert.DeserializeObject<SeqCliConfig>(content, SerializerSettings)!;
    }

    public static void Write(SeqCliConfig data, string filename)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (!data._exportable)
            throw new InvalidOperationException("The provided configuration is not exportable.");

        var content = JsonConvert.SerializeObject(data, Formatting.Indented, SerializerSettings);
        File.WriteAllText(filename, content);
    }

    public SeqCliConnectionConfig Connection { get; set; } = new();
    public SeqCliOutputConfig Output { get; set; } = new();
    public Dictionary<string, SeqCliConnectionConfig> Profiles { get; } = new(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Some configuration objects, for example those with environment overrides, should not be exported
    /// back to JSON files. Call this method to mark the current configuration as non-exportable.
    /// </summary>
    public void DisallowExport()
    {
        _exportable = false;
    }
}