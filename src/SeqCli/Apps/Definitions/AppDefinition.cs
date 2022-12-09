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

#nullable enable

using System.Collections.Generic;
using Newtonsoft.Json;

namespace SeqCli.Apps.Definitions;

// ReSharper disable all
class AppDefinition
{
    public AppDefinition(string name)
    {
        Name = name;
    }
        
    public string Name { get; set; }
 
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? Description { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool AllowReprocessing { get; set; }
        
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? Executable { get; set; }
        
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? Arguments { get; set; }
        
    public List<string> Capabilities { get; set; } = new List<string>();
        
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, AppPlatformDefinition>? Platform { get; set; }
        
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, AppSettingDefinition>? Settings { get; set; }
}