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

using SeqCli.Config;

namespace SeqCli.Cli.Features;

class OutputFormatFeature : CommandFeature
{
    bool _json;
    bool? _noColor, _forceColor;

    public OutputFormat GetOutputFormat(SeqCliConfig config)
    {
        return new OutputFormat(_json, _noColor ?? config.Output.DisableColor, _forceColor ?? config.Output.ForceColor);
    }

    public override void Enable(OptionSet options)
    {
        options.Add(
            "json",
            "Print output in newline-delimited JSON (the default is plain text)",
            _ => _json = true);

        options.Add("no-color", "Don't colorize text output", _ => _noColor = true);

        options.Add("force-color",
            "Force redirected output to have ANSI color (unless `--no-color` is also specified)",
            _ => _forceColor = true);
    }
}