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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Seq.Syntax.Expressions;
using Seq.Syntax.Templates.Compilation;
using Seq.Syntax.Templates.Compilation.NameResolution;
using Seq.Syntax.Templates.Encoding;
using Seq.Syntax.Templates.Parsing;
using Serilog.Events;
using Serilog.Formatting;

// ReSharper disable MemberCanBePrivate.Global, UnusedMember.Global

namespace Seq.Syntax.Templates;

/// <summary>
/// Formats <see cref="LogEvent"/>s into text using embedded expressions.
/// </summary>
public class ExpressionTemplate : ITextFormatter
{
    readonly CompiledTemplate _compiled;

    /// <summary>
    /// Construct an <see cref="ExpressionTemplate"/>.
    /// </summary>
    /// <param name="template">The template text.</param>
    /// <param name="result">The parsed template, if successful.</param>
    /// <param name="error">A description of the error, if unsuccessful.</param>
    /// <returns><c langword="true">true</c> if the template was well-formed.</returns>
    public static bool TryParse(
        string template,
        [MaybeNullWhen(false)] out ExpressionTemplate result,
        [MaybeNullWhen(true)] out string error)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        return TryParse(template, null, null, null, out result, out error);
    }

    /// <summary>
    /// Construct an <see cref="ExpressionTemplate"/>.
    /// </summary>
    /// <param name="template">The template text.</param>
    /// <param name="culture">Optionally, a <see cref="CultureInfo"/> to use when formatting
    /// embedded values.</param>
    /// <param name="result">The parsed template, if successful.</param>
    /// <param name="error">A description of the error, if unsuccessful.</param>
    /// <param name="nameResolver">Optionally, a <see cref="NameResolver"/>
    /// with which to resolve function names that appear in the template.</param>
    /// <param name="encoder">Optionally, an encoder to apply to output substituted into template holes.</param>
    /// <returns><c langword="true">true</c> if the template was well-formed.</returns>
    public static bool TryParse(
        string template,
        CultureInfo? culture,
        NameResolver? nameResolver,
        TemplateOutputEncoder? encoder,
        [MaybeNullWhen(false)] out ExpressionTemplate result,
        [MaybeNullWhen(true)] out string error)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));

        var templateParser = new TemplateParser();
        if (!templateParser.TryParse(template, out var parsed, out error))
        {
            result = null;
            return false;
        }

        var planned = TemplateLocalNameBinder.BindLocalValueNames(parsed);

        result = new ExpressionTemplate(
            TemplateCompiler.Compile(
                planned,
                culture,
                TemplateFunctionNameResolver.Build(nameResolver, planned),
                new EncodedTemplateFactory(encoder)));

        return true;
    }

    ExpressionTemplate(CompiledTemplate compiled)
    {
        _compiled = compiled;
    }

    /// <summary>
    /// Construct an <see cref="ExpressionTemplate"/>.
    /// </summary>
    /// <param name="template">The template text.</param>
    /// <param name="culture">Optionally, a <see cref="CultureInfo"/> to use when formatting
    /// embedded values.</param>
    /// <param name="nameResolver">Optionally, a <see cref="NameResolver"/>
    /// with which to resolve function names that appear in the template.</param>
    /// <param name="encoder">Optionally, an encoder to apply to output substituted into template holes.</param>
    public ExpressionTemplate(
        string template,
        CultureInfo? culture = null,
        NameResolver? nameResolver = null,
        TemplateOutputEncoder? encoder = null)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));

        var templateParser = new TemplateParser();
        if (!templateParser.TryParse(template, out var parsed, out var error))
            throw new ArgumentException(error);

        var planned = TemplateLocalNameBinder.BindLocalValueNames(parsed);

        _compiled = TemplateCompiler.Compile(
            planned,
            culture,
            TemplateFunctionNameResolver.Build(nameResolver, planned),
            new EncodedTemplateFactory(encoder));
    }


    /// <inheritdoc />
    public void Format(LogEvent logEvent, TextWriter output)
    {
        _compiled.Evaluate(new EvaluationContext(logEvent), output);
    }

    /// <summary>
    /// Escape <paramref name="text"/> so that it will be interpreted as
    /// literal text when incorporated into an expression template.
    /// </summary>
    /// <param name="text">The text to apply escaping to.</param>
    /// <returns>The text with any template special characters escaped.</returns>
    public static string EscapeLiteralText(string text)
    {
        return text.Replace("{", "{{").Replace("}", "}}");
    }
}