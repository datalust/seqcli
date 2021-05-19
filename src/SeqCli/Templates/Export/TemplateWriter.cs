using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seq.Api.Model;

#nullable enable

namespace SeqCli.Templates.Export
{
    static class TemplateWriter
    {
        public const string TemplateFileExtension = "template";
        
        public static async Task WriteTemplateAsync(string filename, Entity entity, TemplateValueMap templateValueMap)
        {
            await using var f = File.Create(filename);
            await using var w = new StreamWriter(f, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            using var jw = new JsonTextWriter(w)
            {
                Formatting = Formatting.Indented,
                FloatFormatHandling = FloatFormatHandling.String,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Culture = CultureInfo.InvariantCulture
            };

            await WriteObjectAsync(jw, entity, templateValueMap);
        }

        static async Task WriteValueAsync(JsonWriter jw, object? v, TemplateValueMap templateValueMap)
        {
            await (v switch
            {
                null => jw.WriteNullAsync(),
                string s => jw.WriteValueAsync(s),
                IEnumerable a => WriteArrayAsync(jw, a, templateValueMap),
                var o when o.GetType().IsClass => WriteObjectAsync(jw, o, templateValueMap),
                var e when e.GetType().IsEnum => jw.WriteValueAsync(e.ToString()),
                _ => jw.WriteValueAsync(v)
            });
        }

        static async Task WriteArrayAsync(JsonWriter jw, object a, TemplateValueMap templateValueMap)
        {
            await jw.WriteStartArrayAsync();

            foreach (var v in (IEnumerable)a)
            {
                await WriteValueAsync(jw, v, templateValueMap);
            }
            
            await jw.WriteEndArrayAsync();
        }

        static async Task WriteObjectAsync(JsonWriter jw, object o, TemplateValueMap templateValueMap)
        {
            await jw.WriteStartObjectAsync();

            foreach (var (pi, v) in GetTemplateProperties(o))
            {
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
                    await WriteValueAsync(jw, v, templateValueMap);
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
}