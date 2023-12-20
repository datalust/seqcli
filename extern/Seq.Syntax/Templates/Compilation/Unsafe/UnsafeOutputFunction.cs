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
using System.Reflection;
using Seq.Syntax.Expressions;
using Seq.Syntax.Templates.Encoding;
using Serilog.Events;

namespace Seq.Syntax.Templates.Compilation.Unsafe;

/// <summary>
/// This little extension implements the <c>rest()</c> function in expression templates. It's based on
/// <c>Serilog.Sinks.SystemConsole.PropertiesTokenRenderer</c>, and is equivalent to how <c>Properties</c> is rendered by
/// the console sink. <c>rest()</c> will return a structure containing all of the user-defined properties from a
/// log event except those referenced in either the event's message template, or the expression template itself.
/// </summary>
/// <remarks>
/// The existing semantics of <c>Properties</c> in output templates isn't suitable for expression templates. The
/// <c>@p</c> object provides access to <em>all</em> event properties in an expression template, so it would make no
/// sense to render that object without all of its members.
/// </remarks>
class UnsafeOutputFunction : NameResolver
{
    const string FunctionName = "unsafe";

    public override bool TryResolveFunctionName(string name, [MaybeNullWhen(false)] out MethodInfo implementation)
    {
        if (name.Equals(FunctionName, StringComparison.OrdinalIgnoreCase))
        {
            implementation = typeof(UnsafeOutputFunction).GetMethod(nameof(Implementation),
                BindingFlags.Static | BindingFlags.Public)!;
            return true;
        }

        implementation = null;
        return false;
    }

    // By convention, built-in functions accept and return nullable values.
    // ReSharper disable once ReturnTypeCanBeNotNullable
    public static LogEventPropertyValue? Implementation(LogEventPropertyValue? inner)
    {
        return new ScalarValue(new PreEncodedValue(inner));
    }
}