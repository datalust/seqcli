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

using System;
using System.Globalization;
using System.Linq;
using Seq.Syntax.BuiltIns;
using Seq.Syntax.Expressions;
using Seq.Syntax.Expressions.Ast;
using Seq.Syntax.Expressions.Compilation;
using Seq.Syntax.Templates.Ast;
using Seq.Syntax.Templates.Encoding;

namespace Seq.Syntax.Templates.Compilation;

static class TemplateCompiler
{
    public static CompiledTemplate Compile(Template template,
        CultureInfo? culture, NameResolver nameResolver,
        EncodedTemplateFactory encoder)
    {
        return template switch
        {
            LiteralText text => new CompiledLiteralText(text.Text),
            FormattedExpression { Expression: AmbientNameExpression { IsBuiltIn: true, PropertyName: BuiltInProperty.Level } } level =>
                encoder.Wrap(new CompiledLevelToken(level.Format, level.Alignment)),
            FormattedExpression
            {
                Expression: AmbientNameExpression { IsBuiltIn: true, PropertyName: BuiltInProperty.Exception },
                Alignment: null,
                Format: null
            } => encoder.Wrap(new CompiledExceptionToken()),
            FormattedExpression
            {
                Expression: AmbientNameExpression { IsBuiltIn: true, PropertyName: BuiltInProperty.Message },
                Format: null
            } message => encoder.Wrap(new CompiledMessageToken(culture, message.Alignment)),
            FormattedExpression expression => encoder.MakeCompiledFormattedExpression(
                ExpressionCompiler.Compile(expression.Expression, culture, nameResolver), expression.Format, expression.Alignment, culture),
            TemplateBlock block => new CompiledTemplateBlock(block.Elements.Select(e => Compile(e, culture, nameResolver, encoder)).ToArray()),
            Conditional conditional => new CompiledConditional(
                ExpressionCompiler.Compile(conditional.Condition, culture, nameResolver),
                Compile(conditional.Consequent, culture, nameResolver, encoder),
                conditional.Alternative == null ? null : Compile(conditional.Alternative, culture, nameResolver, encoder)),
            Repetition repetition => new CompiledRepetition(
                ExpressionCompiler.Compile(repetition.Enumerable, culture, nameResolver),
                repetition.BindingNames.Length > 0 ? repetition.BindingNames[0] : null,
                repetition.BindingNames.Length > 1 ? repetition.BindingNames[1] : null,
                Compile(repetition.Body, culture, nameResolver, encoder),
                repetition.Delimiter == null ? null : Compile(repetition.Delimiter, culture, nameResolver, encoder),
                repetition.Alternative == null ? null : Compile(repetition.Alternative, culture, nameResolver, encoder)),
            _ => throw new NotSupportedException()
        };
    }
}