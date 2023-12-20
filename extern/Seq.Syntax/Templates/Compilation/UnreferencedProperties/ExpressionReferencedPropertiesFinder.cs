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

using System.Collections.Generic;
using System.Linq;
using Seq.Syntax.Expressions.Ast;
using Seq.Syntax.Expressions.Compilation.Transformations;

namespace Seq.Syntax.Templates.Compilation.UnreferencedProperties;

class ExpressionReferencedPropertiesFinder : SerilogExpressionTransformer<IEnumerable<string>>
{
    public IEnumerable<string> FindReferencedProperties(Expression expression)
    {
        return Transform(expression);
    }

    protected override IEnumerable<string> Transform(CallExpression call)
    {
        return call.Operands.SelectMany(Transform);
    }

    protected override IEnumerable<string> Transform(ConstantExpression cx)
    {
        yield break;
    }

    protected override IEnumerable<string> Transform(AmbientNameExpression px)
    {
        if (!px.IsBuiltIn)
            yield return px.PropertyName;
    }

    protected override IEnumerable<string> Transform(LocalNameExpression nlx)
    {
        yield break;
    }

    protected override IEnumerable<string> Transform(AccessorExpression spx)
    {
        return Transform(spx.Receiver);
    }

    protected override IEnumerable<string> Transform(LambdaExpression lmx)
    {
        return Transform(lmx.Body);
    }

    protected override IEnumerable<string> Transform(ParameterExpression prx)
    {
        yield break;
    }

    protected override IEnumerable<string> Transform(IndexerWildcardExpression wx)
    {
        yield break;
    }

    protected override IEnumerable<string> Transform(ArrayExpression ax)
    {
        return ax.Elements.OfType<ItemElement>().SelectMany(i => Transform(i.Value))
            .Concat(ax.Elements.OfType<SpreadElement>().SelectMany(i => Transform(i.Content)));
    }

    protected override IEnumerable<string> Transform(ObjectExpression ox)
    {
        return ox.Members.OfType<PropertyMember>().SelectMany(m => Transform(m.Value))
            .Concat(ox.Members.OfType<SpreadMember>().SelectMany(m => Transform(m.Content)));
    }

    protected override IEnumerable<string> Transform(IndexerExpression ix)
    {
        return Transform(ix.Index).Concat(Transform(ix.Receiver));
    }

    protected override IEnumerable<string> Transform(IndexOfMatchExpression mx)
    {
        return Transform(mx.Corpus);
    }
}