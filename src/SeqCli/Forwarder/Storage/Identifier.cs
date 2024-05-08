// Copyright Datalust Pty Ltd
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
using System.Globalization;

namespace SeqCli.Forwarder.Storage;

/// <summary>
///     Utilities for parsing and formatting file names with sortable identifiers.
/// </summary>
public static class Identifier
{
    /// <summary>
    ///     Try parse the identifier from the given name with the given extension.
    /// </summary>
    public static bool TryParse(string name, string extension, [NotNullWhen(true)] out ulong? parsed)
    {
        if (name.Length != 16 + extension.Length)
        {
            parsed = null;
            return false;
        }

        if (!name.EndsWith(extension))
        {
            parsed = null;
            return false;
        }

        if (ulong.TryParse(name.AsSpan()[..16], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id))
        {
            parsed = id;
            return true;
        }

        parsed = null;
        return false;
    }

    /// <summary>
    ///     Format an identifier with the given identifier and extension.
    /// </summary>
    public static string Format(ulong id, string extension)
    {
        return $"{id:x16}{extension}";
    }
}
