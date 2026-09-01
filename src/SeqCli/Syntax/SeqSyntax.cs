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
using System.Diagnostics.CodeAnalysis;
using Seq.Syntax.Expressions;
using Seq.Syntax.Templates;
using Seq.Syntax.Templates.Encoding;
using SeqCli.Syntax.V1;
using V1Compatibility = Seq.Syntax.Compatibility.V1;

namespace SeqCli.Syntax;

/// <summary>
/// Compiles the expressions and templates accepted on the command line. Uses the Seq.Syntax v1
/// compatibility shim so that established seqcli syntax — abbreviated built-in names like
/// <c>@l</c>, and the <c>Elapsed()</c>/<c>Milliseconds()</c> functions — keeps working.
/// </summary>
static class SeqSyntax
{
    public static CompiledExpression CompileExpression(string expression)
    {
        if (!TryCompileExpression(expression, out var compiled, out var error))
            throw new ArgumentException(error);

        return compiled;
    }

    public static bool TryCompileExpression(
        string expression,
        [MaybeNullWhen(false)] out CompiledExpression result,
        [MaybeNullWhen(true)] out string error)
    {
        return V1Compatibility.TryCompileExpression(expression, formatProvider: null, TracingFunctions.Resolver, out result, out error);
    }

    public static ExpressionTemplate ParseTemplate(string template, TemplateOutputEncoder? encoder = null)
    {
        if (!V1Compatibility.TryParseTemplate(template, culture: null, TracingFunctions.Resolver, encoder, out var parsed, out var error))
            throw new ArgumentException(error);

        return parsed;
    }
}
