// Copyright © Datalust Pty Ltd
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

using SeqCli.Cli.Features;

namespace SeqCli.Config;

static class RuntimeConfigurationLoader
{
    const string DefaultEnvironmentVariablePrefix = "SEQCLI_";
    
    /// <summary>
    /// This is the method to use when loading configuration for runtime use. It will read the default configuration
    /// file, if any, and apply overrides from the environment.
    /// </summary>
    public static SeqCliConfig Load(StoragePathFeature storage)
    {
        var config = SeqCliConfig.ReadFromFile(storage.ConfigFilePath);
        
        EnvironmentOverrides.Apply(DefaultEnvironmentVariablePrefix, config);
        
        return config;
    }    
}