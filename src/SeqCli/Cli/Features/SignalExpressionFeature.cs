// Copyright © Datalust and contributors.
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

using Seq.Api.Model.Signals;
using SeqCli.Signals;

namespace SeqCli.Cli.Features;

class SignalExpressionFeature : CommandFeature
{
    string? _signalExpression;

    public SignalExpressionPart? Signal
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_signalExpression))
                return null;

            return SignalExpressionParser.ParseExpression(_signalExpression);
        }
    }

    public override void Enable(OptionSet options)
    {
        options.Add(
            "signal=",
            "A signal expression or list of intersected signal ids to apply, for example `signal-1,signal-2`",
            v => _signalExpression = v);
    }
}