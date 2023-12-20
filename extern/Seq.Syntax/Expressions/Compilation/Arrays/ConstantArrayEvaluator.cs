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

using System.Linq;
using Seq.Syntax.Expressions.Ast;
using Seq.Syntax.Expressions.Compilation.Transformations;
using Serilog.Events;

namespace Seq.Syntax.Expressions.Compilation.Arrays;

class ConstantArrayEvaluator : IdentityTransformer
{
    static readonly ConstantArrayEvaluator Instance = new();

    public static Expression Evaluate(Expression expression)
    {
        return Instance.Transform(expression);
    }

    protected override Expression Transform(ArrayExpression ax)
    {
        // This should probably go depth-first.

        if (ax.Elements.All(el => el is ItemElement { Value: ConstantExpression }))
        {
            return new ConstantExpression(
                new SequenceValue(ax.Elements
                    .Cast<ItemElement>()
                    .Select(item => ((ConstantExpression)item.Value).Constant)));
        }

        return base.Transform(ax);
    }
}