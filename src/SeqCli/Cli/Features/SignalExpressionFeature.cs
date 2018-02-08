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

using Seq.Api.Model.Signals;

namespace SeqCli.Cli.Features
{
    class SignalExpressionFeature : CommandFeature
    {
        string _signalExpression;

        public SignalExpressionPart Signal
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_signalExpression))
                    return null;

                // This is a hack that just happens to work because of the way
                // signal ids are passed through ToString() as literals
                return SignalExpressionPart.Signal(_signalExpression.Trim());
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
}
