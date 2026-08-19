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
using SeqCli.Ingestion;
using SeqCli.Mapping;
using Serilog.Expressions;
using Serilog.Formatting;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace SeqCli.Output;

// This is the only usage of Serilog.Expressions remaining in seqcli; the upstream Seq.Syntax doesn't yet support
// tracing properties or theming.
static class TextFormatters
{
    public static ITextFormatter Json(TemplateTheme? theme) => new ExpressionTemplate(
        $"{{ " +
        $"if {MetricsMapping.SurrogateDefinitionsProperty} is not null then " +
        // Emit a metric sample
        $"{{@t, @l: undefined(), @d: {MetricsMapping.SurrogateDefinitionsProperty}, ..rest()}} " +
        $"else " +
        // Emit a log or span
        $"{{@t, @mt, @l: coalesce({LevelMapping.SurrogateLevelProperty}, if @l = 'Information' then undefined() else @l), @x, @sp, @tr, @ps: coalesce({TraceConstants.ParentSpanIdProperty}, @ps), @st: coalesce({TraceConstants.SpanStartTimestampProperty}, @st), ..rest()}} " +
        $"}}" +
        Environment.NewLine,
        theme: theme,
        // The `OutputFormat` constructor has already decided whether to colorize.
        applyThemeWhenOutputIsRedirected: true
    );

    static readonly string DefaultPlainTextOutputTemplate =
        "[{@t:o} {@l:u3}] {@m}{#if IsSpan()} ({Milliseconds(Elapsed()):0.###} ms){#end}" + Environment.NewLine + "{@x}";

    public static ITextFormatter Plain(TemplateTheme? theme, string? outputTemplate) => new ExpressionTemplate(
        outputTemplate ?? DefaultPlainTextOutputTemplate,
        theme: theme,
        nameResolver: new StaticMemberNameResolver(typeof(TracingFunctions)),
        // The `OutputFormat` constructor has already decided whether to colorize.
        applyThemeWhenOutputIsRedirected: true
    );
}