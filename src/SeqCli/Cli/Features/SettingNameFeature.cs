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
using Seq.Api.Model.Settings;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Features;

class SettingNameFeature: CommandFeature
{
    string? _name;

    public SettingName Name => Enum.Parse<SettingName>(_name!, ignoreCase: true);

    public override void Enable(OptionSet options)
    {
        options.Add("n|name=", "The setting name, for example `OpenIdConnectClientSecret`", k => _name = ArgumentString.Normalize(k));
    }

    public override IEnumerable<string> GetUsageErrors()
    {
        if (string.IsNullOrEmpty(_name))
        {
            yield return "A setting must be specified with `--name=NAME`.";
        }
        
        if (!Enum.TryParse(_name, ignoreCase: true, out SettingName _))
        {
            yield return $"The setting name {_name} was not recognized; run the `seqcli setting names` command to see supported settings.";
        }
    }
}