﻿// Copyright 2020 Datalust Pty Ltd and Contributors
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

using Newtonsoft.Json;

namespace SeqCli.Apps.Definitions
{
    // ReSharper disable all
    class AppPlatformDefinition
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Executable { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Arguments { get; set; }
        
        // The generic host for assembly-based apps uses this.
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SeqAppTypeName { get; set; }
    }
}
