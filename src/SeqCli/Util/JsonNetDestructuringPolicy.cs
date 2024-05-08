// Copyright 2015 Destructurama Contributors
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using Serilog.Events;

namespace SeqCli.Util;

sealed class JsonNetDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        switch (value)
        {
            case JObject jo:
                result = Destructure(jo, propertyValueFactory);
                return true;
            case JArray ja:
                result = Destructure(ja, propertyValueFactory);
                return true;
            case JValue jv:
                result = Destructure(jv, propertyValueFactory);
                return true;
        }

        result = null;
        return false;
    }

    static LogEventPropertyValue Destructure(JValue jv, ILogEventPropertyValueFactory propertyValueFactory)
    {
        return propertyValueFactory.CreatePropertyValue(jv.Value!, destructureObjects: true);
    }

    static SequenceValue Destructure(JArray ja, ILogEventPropertyValueFactory propertyValueFactory)
    {
        var elems = ja.Select(t => propertyValueFactory.CreatePropertyValue(t, destructureObjects: true));
        return new SequenceValue(elems);
    }

    static LogEventPropertyValue Destructure(JObject jo, ILogEventPropertyValueFactory propertyValueFactory)
    {
        string? typeTag = null;
        var props = new List<LogEventProperty>(jo.Count);

        foreach (var prop in jo.Properties())
        {
            if (prop.Name == "$type")
            {
                if (prop.Value is JValue typeVal && typeVal.Value is string v)
                {
                    typeTag = v;
                    continue;
                }
            }
            else if (!LogEventProperty.IsValidName(prop.Name))
            {
                return DestructureToDictionaryValue(jo, propertyValueFactory);
            }

            props.Add(new LogEventProperty(prop.Name, propertyValueFactory.CreatePropertyValue(prop.Value, destructureObjects: true)));
        }

        return new StructureValue(props, typeTag);
    }

    static DictionaryValue DestructureToDictionaryValue(JObject jo, ILogEventPropertyValueFactory propertyValueFactory)
    {
        var elements = jo.Properties().Select(
            prop => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                    new ScalarValue(prop.Name),
                    propertyValueFactory.CreatePropertyValue(prop.Value, destructureObjects: true))
        );
        return new DictionaryValue(elements);
    }
}