﻿// Copyright © Serilog Contributors
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

using System.IO;
using Seq.Syntax.Expressions;

namespace Seq.Syntax.Templates.Compilation;

class CompiledExceptionToken : CompiledTemplate
{
    public override void Evaluate(EvaluationContext ctx, TextWriter output)
    {
        // Padding and alignment are not applied by this renderer.

        if (ctx.LogEvent.Exception is null)
            return;

        var lines = new StringReader(ctx.LogEvent.Exception.ToString());
        string? nextLine;
        while ((nextLine = lines.ReadLine()) != null)
        {
            output.WriteLine(nextLine);
        }
    }
}