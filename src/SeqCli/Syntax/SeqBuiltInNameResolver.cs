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

using System.Diagnostics.CodeAnalysis;
using Serilog.Expressions;

namespace SeqCli.Syntax;

/// <summary>
/// Extends Serilog.Expressions with support for commonly-used Seq property names.
/// </summary>
/// <remarks>SeqCli ingestion filters previously used Serilog.Filters.Expressions, so this type ensures most
/// older filters will continue to work.</remarks>
public class SeqBuiltInNameResolver: NameResolver
{
    public override bool TryResolveBuiltInPropertyName(string alias, [NotNullWhen(true)] out string? target)
    {
        switch (alias)
        {
            case "Properties":
                target = "p";
                return true;
            case "Timestamp":
                target = "t";
                return true;
            case "Level":
                target = "l";
                return true;
            case "Message":
                target = "m";
                return true;
            case "MessageTemplate":
                target = "mt";
                return true;
            case "EventType":
                target = "i";
                return true;
            case "Exception":
                target = "x";
                return true;
            default:
                target = null;
                return false;
        }
    }
}
