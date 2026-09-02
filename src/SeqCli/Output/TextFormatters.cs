// Copyright © Datalust Pty Ltd
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
using Seq.Syntax.Templates;
using Seq.Syntax.Templates.Encoding;
using Seq.Syntax.Templates.Themes;
using SeqCli.Syntax;

namespace SeqCli.Output;

static class TextFormatters
{
    /// <summary>
    /// Newline-delimited CLEF output: the event JSON document is written verbatim, with theming
    /// when a theme is supplied.
    /// </summary>
    public static ExpressionTemplate Json(TemplateTheme? theme) => new(
        "{@Data}" + Environment.NewLine,
        encoder: Encoder(theme));

    // Guarding on `@Elapsed` rather than the built-in `IsSpan()` shows elapsed time for any
    // event carrying a span start timestamp, whether or not trace and span ids accompany it.
    static readonly string DefaultPlainTextOutputTemplate =
        "[{@Timestamp:o} {@Level:u3}] {@Message}{#if @Elapsed is not null} ({TotalMilliseconds(@Elapsed):0.###} ms){#end}" +
        Environment.NewLine + "{@Exception}";

    public static ExpressionTemplate Plain(TemplateTheme? theme, string? outputTemplate) =>
        SeqSyntax.ParseTemplate(outputTemplate ?? DefaultPlainTextOutputTemplate, Encoder(theme));

    static TemplateOutputEncoder? Encoder(TemplateTheme? theme) =>
        theme != null ? TemplateOutputEncoder.Ansi(theme) : null;
}
