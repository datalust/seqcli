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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Seq.Api.Model;
using Seq.Api.Model.Data;
using Seq.Api.Model.Events;
using SeqCli.Config;
using SeqCli.Csv;
using SeqCli.Mapping;
using SeqCli.Util;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Templates.Themes;

namespace SeqCli.Output;

sealed class OutputFormat
{
    // See https://no-color.org for semantics.
    const string NoColorEnvironmentVariable = "NO_COLOR";
    
    readonly OutputSyntax _syntax;
    readonly string? _outputTemplate;
    readonly Logger _formatter;

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
        string? outputTemplate = null)
        : this(
            syntax,
            noColor,
            forceColor,
            outputConfig,
            outputTemplate,
            noColorSetInEnvironment: NoColorSetInEnvironment(),
            outputIsRedirected: Console.IsOutputRedirected,
            allowAnsiEscapes: TerminalFeatures.TryEnableAnsiEscapes())
    {
    }

    /// <param name="syntax">The syntax to write output in.</param>
    /// <param name="noColor">The value of <c>--no-color</c>, if specified.</param>
    /// <param name="forceColor">The value of <c>--force-color</c>, if specified.</param>
    /// <param name="outputConfig">Configured output defaults.</param>
    /// <param name="outputTemplate">The template controlling plain-text formatting, or <c>null</c> for the default.</param>
    /// <param name="noColorSetInEnvironment">Whether <c>NO_COLOR</c> is set; see <see cref="NoColorSetInEnvironment"/>.</param>
    /// <param name="outputIsRedirected">Whether <c>STDOUT</c> is redirected, i.e. not attached to a terminal.</param>
    /// <param name="allowAnsiEscapes">Whether ANSI escape sequences are allowed; generally <c>false</c> for interactive
    /// legacy Windows terminals and <c>true</c> otherwise.</param>
    internal OutputFormat(
        OutputSyntax syntax,
        bool? noColor,
        bool? forceColor,
        SeqCliOutputConfig outputConfig,
        string? outputTemplate,
        bool noColorSetInEnvironment,
        bool outputIsRedirected,
        bool allowAnsiEscapes)
    {
        _syntax = syntax;
        _outputTemplate = outputTemplate;

        var resolvedNoColor = ResolveNoColor(noColor, forceColor, outputConfig, noColorSetInEnvironment, allowAnsiEscapes);
        var applyThemeToRedirectedOutput = !resolvedNoColor && (forceColor ?? outputConfig.ForceColor);
        var colorize = !resolvedNoColor && (applyThemeToRedirectedOutput || !outputIsRedirected);

        TemplateTheme = colorize
            ? FlareTheme.SeqCli
            : null;

        _formatter = CreateOutputLogger();
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

    Logger CreateOutputLogger()
    {
        var outputConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .Enrich.With<RedundantEventTypeRemovalEnricher>();

        if (Json)
        {
            outputConfiguration.WriteTo.Console(TextFormatters.Json(TemplateTheme));
        }
        else if (Text)
        {
            outputConfiguration.WriteTo.Console(TextFormatters.Plain(TemplateTheme, _outputTemplate));
        }
        
        // The logger is not configured for Native output, which avoids it. Ideally we'll shift away from using
        // Serilog here, and move Text/Json over to EventEntity-driven formatters, too.

        return outputConfiguration.CreateLogger();
    }

    public void WriteEntity(Entity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var jo = JObject.FromObject(
            entity,
            _serializer);
            
        if (Json)
        {
            jo.Remove("Links");
                
            var writer = new LoggerConfiguration()
                .Destructure.With<JsonNetDestructuringPolicy>()
                .Enrich.With<StripStructureTypeEnricher>()
                .WriteTo.Console(TextFormatters.Plain(TemplateTheme, "{@m}" + Environment.NewLine))
                .CreateLogger();
            writer.Information("{@Entity}", jo);
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

            // Using the same method of JSON colorization as above

            var writer = new LoggerConfiguration()
                .Destructure.With<JsonNetDestructuringPolicy>()
                .Enrich.With<StripStructureTypeEnricher>()
                .WriteTo.Console(TextFormatters.Plain(TemplateTheme, "{@m}" + Environment.NewLine))
                .CreateLogger();
            writer.Information("{@Entity}", jo);
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
            var serilogEvent = ToSerilogEvent(evt);

            if (Text)
            {
                // Add flattened versions of structured properties that are referenced using dotted-name syntax in
                // message templates, e.g. <c>{user.name}</c>. Serilog.Expressions template rendering doesn't otherwise
                // support these. In text output mode, these aren't usually observable, though
                // <c>seqcli print --template="{@p}"</c> will make them visible.
                FlattenPropertiesUsedWithDottedNames(evt, serilogEvent);
            }

            WriteLogEvent(serilogEvent);
        }
    }

    public void WriteLogEvent(LogEvent logEvent)
    {
        _formatter.Write(logEvent);
    }

    public static LogEvent ToSerilogEvent(EventEntity evt)
    {
        ActivityTraceId traceId = default;
        if (!string.IsNullOrWhiteSpace(evt.TraceId))
            traceId = ActivityTraceId.CreateFromString(evt.TraceId);

        ActivitySpanId spanId = default;
        if (!string.IsNullOrWhiteSpace(evt.SpanId))
            spanId = ActivitySpanId.CreateFromString(evt.SpanId);

        var serilogEvent = new LogEvent(
            DateTimeOffset.ParseExact(evt.Timestamp, "o", CultureInfo.InvariantCulture).ToLocalTime(),
            LevelMapping.ToSerilogLevel(evt.Level),
            string.IsNullOrWhiteSpace(evt.Exception) ? null : new TextException(evt.Exception),
            new MessageTemplate(evt.MessageTemplateTokens.Select(ToMessageTemplateToken)),
            evt.Properties
                .Select(p => CreateProperty(p.Name, p.Value)),
            traceId,
            spanId
        );

        if (evt.Scope?.Count > 0)
            serilogEvent.AddOrUpdateProperty(new("@sa", new StructureValue(evt.Scope.Select(p => CreateProperty(p.Name, p.Value)))));

        if (evt.Resource?.Count > 0)
            serilogEvent.AddOrUpdateProperty(new("@ra", new StructureValue(evt.Resource.Select(p => CreateProperty(p.Name, p.Value)))));

        if (!string.IsNullOrWhiteSpace(evt.ParentId))
            serilogEvent.AddOrUpdateProperty(new("@ps", new ScalarValue(evt.ParentId)));

        if (!string.IsNullOrWhiteSpace(evt.Start))
            serilogEvent.AddOrUpdateProperty(new("@st", new ScalarValue(evt.Start)));

        if (!string.IsNullOrWhiteSpace(evt.SpanKind))
            serilogEvent.AddOrUpdateProperty(new("@sk", new ScalarValue(evt.SpanKind)));
        
        return serilogEvent;
    }

    public static void FlattenPropertiesUsedWithDottedNames(EventEntity evt, LogEvent serilogEvent)
    {
        foreach (var token in evt.MessageTemplateTokens)
        {
            if (token.Text != null || token.PropertyName is not { } name || !name.Contains('.') ||
                serilogEvent.Properties.ContainsKey(name))
            {
                continue;
            }

            var steps = name.Split('.');
            var value = evt.Properties.FirstOrDefault(p => p.Name == steps[0])?.Value;
            for (var i = 1; i < steps.Length; ++i)
            {
                value = (value as JObject)?.GetValue(steps[i]);
            }

            if (value is JToken resolved)
            {
                // Existing flat-named properties, where present, win.
                serilogEvent.AddPropertyIfAbsent(LogEventPropertyFactory.SafeCreate(
                    name, resolved is JValue scalar ? new ScalarValue(scalar.Value) : CreatePropertyValue(resolved)));
            }
        }
    }

    static MessageTemplateToken ToMessageTemplateToken(MessageTemplateTokenPart token)
    {
        // Not ideal, we lose renderings, alignment etc. here.

        if (token.Text != null)
            return new TextToken(token.Text);
        return new PropertyToken(token.PropertyName, token.RawText ?? $"{{{token.PropertyName}}}");
    }

    static LogEventProperty CreateProperty(string name, object value)
    {
        return LogEventPropertyFactory.SafeCreate(name, CreatePropertyValue(value));
    }

    internal static LogEventPropertyValue CreatePropertyValue(object value)
    {
        switch (value)
        {
            case JObject jo:
                jo.TryGetValue("$typeTag", out var tt);
                return new StructureValue(
                    jo.Properties()
                        .Where(kvp => kvp.Name != "$typeTag")
                        .Select(kvp => CreateProperty(kvp.Name, kvp.Value)),
                    (tt as JValue)?.Value as string);

            case JArray ja:
                return new SequenceValue(ja.Select(CreatePropertyValue));

            default:
                return new ScalarValue(value);
        }
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
