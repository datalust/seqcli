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

using System.Collections.Generic;

namespace SeqCli.Apps.Definitions
{
    class AppDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool AllowReprocessing { get; set; }
        public string Executable { get; set; }
        public string Arguments { get; set; }
        public List<string> Capabilities { get; set; } = new List<string>();
        public Dictionary<string, AppPlatformDefinition> Platform { get; set; }
        public Dictionary<string, AppSettingDefinition> Settings { get; set; }

        // The interrogator for assembly-based apps uses this.
        // ReSharper disable once InconsistentNaming
        public string __MainReactorTypeName { get; set; }
    }
}
