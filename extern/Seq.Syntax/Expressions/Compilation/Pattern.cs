// Copyright © Serilog Contributors
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

using System.Diagnostics.CodeAnalysis;
using Seq.Syntax.Expressions.Ast;
using Serilog.Events;

namespace Seq.Syntax.Expressions.Compilation;

static class Pattern
{
    public static bool IsAmbientProperty(Expression expression, string name, bool isBuiltIn)
    {
        return expression is AmbientNameExpression px &&
               px.PropertyName == name &&
               px.IsBuiltIn == isBuiltIn;
    }

    public static bool IsStringConstant(Expression expression, [MaybeNullWhen(false)] out string value)
    {
        if (expression is ConstantExpression { Constant: ScalarValue { Value: string s } })
        {
            value = s;
            return true;
        }

        value = null;
        return false;
    }
}