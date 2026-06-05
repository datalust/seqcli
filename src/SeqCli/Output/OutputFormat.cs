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
using SeqCli.Csv;
using SeqCli.Mapping;
using SeqCli.Util;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Templates.Themes;

namespace SeqCli.Output;

sealed class OutputFormat
{
    readonly OutputSyntax _syntax;
    readonly bool _noColor;
    readonly bool _forceColor;
    readonly Logger _formatter;

    public const string DefaultOutputTemplate =
        "[{Timestamp:o} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

    public static readonly ConsoleTheme DefaultAnsiTheme = AnsiConsoleTheme.Code;

    public static readonly ConsoleTheme DefaultTheme =
        OperatingSystem.IsWindows() ? SystemConsoleTheme.Literate : DefaultAnsiTheme;

    static readonly TemplateTheme DefaultTemplateTheme = TemplateTheme.Code;

    public OutputFormat(OutputSyntax syntax, bool noColor, bool forceColor)
    {
        _syntax = syntax;
        _noColor = noColor;
        _forceColor = forceColor;
        _formatter = CreateOutputLogger();
    }

    public bool Json => _syntax == OutputSyntax.Json;
    public bool Text => _syntax == OutputSyntax.Text;
    public bool Native => _syntax == OutputSyntax.Native;
    
    bool ApplyThemeToRedirectedOutput => !_noColor && _forceColor;

    ConsoleTheme Theme
        => _noColor                     ? ConsoleTheme.None
            :  ApplyThemeToRedirectedOutput ? DefaultAnsiTheme
            :                                 DefaultTheme;

    TemplateTheme? TemplateTheme
        => _noColor                     ? null
            :  ApplyThemeToRedirectedOutput ? DefaultTemplateTheme
            :                                 null;

    public bool RequiresRender => Native;

    Logger CreateOutputLogger()
    {
        var outputConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .Enrich.With<RedundantEventTypeRemovalEnricher>();

        if (Json)
        {
            outputConfiguration.WriteTo.Console(OutputFormatter.Json(TemplateTheme));
        }
        else if (Text)
        {
            outputConfiguration.WriteTo.Console(
                outputTemplate: DefaultOutputTemplate,
                theme: Theme,
                applyThemeToRedirectedOutput: ApplyThemeToRedirectedOutput);
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
            JsonSerializer.CreateDefault(new JsonSerializerSettings {
                DateParseHandling = DateParseHandling.None,
                Converters = {
                    new StringEnumConverter()
                }
            }));
            
        if (Json)
        {
            jo.Remove("Links");
                
            var writer = new LoggerConfiguration()
                .Destructure.With<JsonNetDestructuringPolicy>()
                .Enrich.With<StripStructureTypeEnricher>()
                .WriteTo.Console(
                    outputTemplate: "{@Message:j}{NewLine}",
                    theme: Theme,
                    applyThemeToRedirectedOutput: ApplyThemeToRedirectedOutput)
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
            var settings = JsonSerializer.CreateDefault(new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None,
                Converters =
                {
                    new StringEnumConverter()
                }
            });
            
            var jo = value is ICollection and not (IDictionary or JToken) ?
                (JToken)JArray.FromObject(value, settings) :
                JObject.FromObject(value, settings);

            // Using the same method of JSON colorization as above

            var writer = new LoggerConfiguration()
                .Destructure.With<JsonNetDestructuringPolicy>()
                .Enrich.With<StripStructureTypeEnricher>()
                .WriteTo.Console(
                    outputTemplate: "{@Message:j}{NewLine}",
                    theme: Theme,
                    applyThemeToRedirectedOutput: ApplyThemeToRedirectedOutput)
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
            CsvWriter.WriteQueryResult(result, Stringify, Theme, Console.Out);
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
            _formatter.Write(ToSerilogEvent(evt));
        }
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

    static LogEventPropertyValue CreatePropertyValue(object value)
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
