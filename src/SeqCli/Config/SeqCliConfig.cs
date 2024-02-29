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
using SeqCli.Config.Forwarder;

namespace SeqCli.Config;

class SeqCliConfig
{
    static readonly string DefaultConfigFilename =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SeqCli.json");

    static JsonSerializerSettings SerializerSettings { get; } = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters =
        {
            new StringEnumConverter()
        }
    };

    public static SeqCliConfig Read()
    {
        if (!File.Exists(DefaultConfigFilename))
            return new SeqCliConfig();
            
        var content = File.ReadAllText(DefaultConfigFilename);
        return JsonConvert.DeserializeObject<SeqCliConfig>(content, SerializerSettings)!;
    }

    public static void Write(SeqCliConfig data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        var content = JsonConvert.SerializeObject(data, Formatting.Indented, SerializerSettings);
        File.WriteAllText(DefaultConfigFilename, content);
    }

    public ConnectionConfig Connection { get; set; } = new ConnectionConfig();
    public OutputConfig Output { get; set; } = new();
    public ForwarderConfig Forwarder { get; set; } = new();
    public SeqCliEncryptionProviderConfig EncryptionProviderProvider { get; set; } = new SeqCliEncryptionProviderConfig();
    
    public Dictionary<string, ConnectionConfig> Profiles { get; } =
        new Dictionary<string, ConnectionConfig>(StringComparer.OrdinalIgnoreCase);
}