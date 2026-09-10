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

using System;
using System.Collections.Generic;
using System.Linq;
using Seq.Api.Model.Signals;

namespace SeqCli.Signals;

static class SignalExpressionPartExtensions
{
    public static IEnumerable<string> ReferencedSignalIds(this SignalExpressionPart expr)
    {
        return expr.Kind switch
        {
            SignalExpressionKind.Signal => [expr.SignalId],
            SignalExpressionKind.Intersection or SignalExpressionKind.Union => expr.Left.ReferencedSignalIds()
                .Concat(expr.Right.ReferencedSignalIds()),
            _ => throw new ArgumentOutOfRangeException(nameof(expr))
        };
    }
}