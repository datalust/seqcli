using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seq.Api.Model;

namespace SeqCli.Templates.Export;

static class TemplateWriter
{
    public const string TemplateFileExtension = "template";
        
    public static async Task WriteTemplateAsync(TextWriter writer, Entity entity, TemplateValueMap templateValueMap)
    {
        using var jw = new JsonTextWriter(writer)
        {
            Formatting = Formatting.Indented,
            FloatFormatHandling = FloatFormatHandling.String,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Culture = CultureInfo.InvariantCulture
        };

        await WriteObjectAsync(jw, entity, templateValueMap, annotateAsResource: true);
    }

    static async Task WriteValueAsync(JsonWriter jw, object? v, TemplateValueMap templateValueMap, PropertyInfo? enclosingProperty = null)
    {
        await (v switch
        {
            null => jw.WriteNullAsync(),
            string s => jw.WriteValueAsync(s),
            IReadOnlyDictionary<string, string> ds => WriteDictionaryAsync(jw, ds, templateValueMap, enclosingProperty),
            IEnumerable a => WriteArrayAsync(jw, a, templateValueMap, enclosingProperty),
            var o when o.GetType().IsClass => WriteObjectAsync(jw, o, templateValueMap),
            var e when e.GetType().IsEnum => jw.WriteValueAsync(e.ToString()),
            _ => jw.WriteValueAsync(v)
        });
    }

    static async Task WriteArrayAsync(JsonWriter jw, object a, TemplateValueMap templateValueMap, PropertyInfo? enclosingProperty = null)
    {
        await jw.WriteStartArrayAsync();

        foreach (var v in (IEnumerable)a)
        {
            if (enclosingProperty != null &&
                templateValueMap.TryGetRawElement(enclosingProperty, v, out var raw))
            {
                await jw.WriteRawValueAsync(raw);
            }
            else
            {
                await WriteValueAsync(jw, v, templateValueMap);
            }
        }
            
        await jw.WriteEndArrayAsync();
    }

    static async Task WriteDictionaryAsync(JsonWriter jw, IReadOnlyDictionary<string, string> ds, TemplateValueMap templateValueMap, PropertyInfo? enclosingProperty = null)
    {
        await jw.WriteStartObjectAsync();

        foreach (var (k, v) in ds)
        {
            await jw.WritePropertyNameAsync(k);

            if (enclosingProperty != null &&
                templateValueMap.TryGetRawElement(enclosingProperty, v, out var raw))
            {
                await jw.WriteRawValueAsync(raw);
            }
            else
            {
                await WriteValueAsync(jw, v, templateValueMap);
            }
        }
            
        await jw.WriteEndObjectAsync();
    }

    static async Task WriteObjectAsync(JsonWriter jw, object o, TemplateValueMap templateValueMap, bool annotateAsResource = false)
    {
        await jw.WriteStartObjectAsync();

        if (annotateAsResource)
        {
            await jw.WritePropertyNameAsync("$entity");
            await jw.WriteValueAsync(EntityName.FromEntityType(o.GetType()));
        }

        foreach (var (pi, v) in GetTemplateProperties(o))
        {
            if (templateValueMap.IsIgnored(pi))
                continue;
                
            var pa = pi.GetCustomAttribute<JsonPropertyAttribute>();
            if (pa?.DefaultValueHandling == DefaultValueHandling.Ignore &&
                v == null || v as int? == 0 || v as decimal? == 0m || v as uint? == 0)
            {
                continue;
            }
                
            await jw.WritePropertyNameAsync(pa?.PropertyName ?? pi.Name);

            if (templateValueMap.TryGetRawValue(pi, v, out var raw))
                await jw.WriteRawValueAsync(raw);
            else
                await WriteValueAsync(jw, v, templateValueMap, pi);
        }
            
        await jw.WriteEndObjectAsync();
    }

    static IEnumerable<(PropertyInfo, object?)> GetTemplateProperties(object o)
    {
        return o.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty)
            .Where(pi => pi.GetCustomAttribute<JsonIgnoreAttribute>() == null &&
                         pi.GetCustomAttribute<ObsoleteAttribute>() == null)
            .Where(pi => pi.PropertyType != typeof(LinkCollection) && pi.Name != "Id")
            .Select(pi => (pi, pi.GetValue(o)));
    }
}