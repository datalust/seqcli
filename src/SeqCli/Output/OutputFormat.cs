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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Seq.Api.Model;
using Seq.Api.Model.Data;
using Seq.Api.Model.Events;
using Seq.Syntax.Templates;
using Seq.Syntax.Templates.Encoding;
using Seq.Syntax.Templates.Themes;
using SeqCli.Config;
using SeqCli.Csv;
using SeqCli.Mapping;
using SeqCli.Util;

namespace SeqCli.Output;

sealed class OutputFormat
{
    // See https://no-color.org for semantics.
    const string NoColorEnvironmentVariable = "NO_COLOR";

    readonly OutputSyntax _syntax;
    readonly ExpressionTemplate? _eventFormatter;
    readonly ExpressionTemplate _jsonValueFormatter;

    readonly JsonSerializer _serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
    {
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            new StringEnumConverter()
        }
    });

    public OutputFormat(
        OutputSyntax syntax,
        bool? noColor,
        bool? forceColor,
        SeqCliOutputConfig outputConfig,
        string? plainTextTemplate = null)
        : this(
            syntax,
            noColor,
            forceColor,
            outputConfig,
            plainTextTemplate,
            noColorSetInEnvironment: NoColorSetInEnvironment(),
            outputIsRedirected: Console.IsOutputRedirected,
            allowAnsiEscapes: TerminalFeatures.TryEnableAnsiEscapes())
    {
    }

    /// <param name="syntax">The syntax to write output in.</param>
    /// <param name="noColor">The value of <c>--no-color</c>, if specified.</param>
    /// <param name="forceColor">The value of <c>--force-color</c>, if specified.</param>
    /// <param name="outputConfig">Configured output defaults.</param>
    /// <param name="plainTextTemplate">The template controlling plain-text formatting, or <c>null</c> for the default.</param>
    /// <param name="noColorSetInEnvironment">Whether <c>NO_COLOR</c> is set; see <see cref="NoColorSetInEnvironment"/>.</param>
    /// <param name="outputIsRedirected">Whether <c>STDOUT</c> is redirected, i.e. not attached to a terminal.</param>
    /// <param name="allowAnsiEscapes">Whether ANSI escape sequences are allowed; generally <c>false</c> for interactive
    /// legacy Windows terminals and <c>true</c> otherwise.</param>
    internal OutputFormat(
        OutputSyntax syntax,
        bool? noColor,
        bool? forceColor,
        SeqCliOutputConfig outputConfig,
        string? plainTextTemplate,
        bool noColorSetInEnvironment,
        bool outputIsRedirected,
        bool allowAnsiEscapes)
    {
        _syntax = syntax;

        var resolvedNoColor = ResolveNoColor(noColor, forceColor, outputConfig, noColorSetInEnvironment, allowAnsiEscapes);
        var applyThemeToRedirectedOutput = !resolvedNoColor && (forceColor ?? outputConfig.ForceColor);
        var colorize = !resolvedNoColor && (applyThemeToRedirectedOutput || !outputIsRedirected);

        TemplateTheme = colorize
            ? FlareTheme.SeqCli
            : null;

        _eventFormatter = Json
            ? TextFormatters.Json(TemplateTheme)
            : Text
                ? TextFormatters.Plain(TemplateTheme, plainTextTemplate)
                : null;

        _jsonValueFormatter = new ExpressionTemplate(
            "{Value}" + Environment.NewLine,
            encoder: TemplateTheme != null ? TemplateOutputEncoder.Ansi(TemplateTheme) : null);
    }

    static bool NoColorSetInEnvironment()
        => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(NoColorEnvironmentVariable));

    internal static bool ResolveNoColor(
        bool? noColorFlag,
        bool? forceColorFlag,
        SeqCliOutputConfig config,
        bool noColorSetInEnvironment,
        bool supportsAnsiEscapes)
    {
        if (!supportsAnsiEscapes)
            return true;

        if (noColorFlag != null)
            return noColorFlag.Value;

        if (config.DisableColor)
            return true;

        return forceColorFlag != true && noColorSetInEnvironment;
    }

    public bool Json => _syntax == OutputSyntax.Json;
    public bool Text => _syntax == OutputSyntax.Text;
    public bool Native => _syntax == OutputSyntax.Native;

    internal TemplateTheme? TemplateTheme { get; }

    public bool RequiresRender => Native;

    public void WriteEntity(Entity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var jo = JObject.FromObject(
            entity,
            _serializer);

        if (Json)
        {
            jo.Remove("Links");
            WriteJsonValue(JsonNodes.FromNewtonsoft(jo));
        }
        else if (Text)
        {
            var dyn = (dynamic) jo;
            Console.WriteLine($"{entity.Id} {dyn.Title ?? dyn.Name ?? dyn.Username ?? dyn.Expression}");
        }
        else
        {
            throw new InvalidOperationException("Native formatting not supported for entities.");
        }
    }

    public void WriteObject(object value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        if (Json)
        {
            var jo = value is ICollection and not (IDictionary or JToken) ?
                (JToken)JArray.FromObject(value, _serializer) :
                JObject.FromObject(value, _serializer);

            WriteJsonValue(JsonNodes.FromNewtonsoft(jo));
        }
        else if (Text)
        {
            Console.WriteLine(Stringify(value));
        }
        else
        {
            NativeFormatter.WriteValue(Console.Out, value);
            Console.WriteLine();
        }
    }

    void WriteJsonValue(JsonNode? value)
    {
        _jsonValueFormatter.Format(new JsonObject { ["Value"] = value }, Console.Out);
    }

    public void ListEntities(IEnumerable<Entity> list)
    {
        foreach (var entity in list)
        {
            WriteEntity(entity);
        }
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
#pragma warning disable CA1822
    public void WriteText(string? text)
#pragma warning restore CA1822
    {
        Console.WriteLine(text?.TrimEnd());
    }

    public void WriteQueryResult(QueryResultPart result)
    {
        if (Json)
        {
            WriteObject(result);
        }
        else if (Native)
        {
            NativeFormatter.WriteQueryResult(Console.Out, result);
        }
        else
        {
            CsvWriter.WriteQueryResult(result, Stringify, TemplateTheme, Console.Out);
        }
    }

    public void WriteEventEntity(EventEntity evt)
    {
        if (Native)
        {
            NativeFormatter.WriteEvent(Console.Out, evt);
            Console.Out.WriteLine();
        }
        else
        {
            WriteEvent(EventEntityJson.ToEventJson(evt));
        }
    }

    public void WriteEvent(JsonObject eventJson)
    {
        _eventFormatter?.Format(eventJson, Console.Out);
    }

    static string Stringify(object? value)
    {
        return value switch
        {
            null => "null",
            true => "true",
            false => "false",
            decimal
                or double or float or Half
                or byte or ushort or uint or ulong or UInt128 or
                sbyte or short or int or long
                or Int128 => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("o"),
            DateTimeOffset dto => dto.ToString("o"),
            _ => value.ToString() ?? ""
        };
    }
}
