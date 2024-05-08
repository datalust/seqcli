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

using System.Diagnostics.CodeAnalysis;

namespace SeqCli.Forwarder.Storage;

/// <summary>
///     A bookmark file name with its incrementing identifier.
/// </summary>
public readonly record struct BookmarkName
{
    readonly string _name;

    public readonly ulong Id;

    public BookmarkName(ulong id)
    {
        Id = id;
        _name = Identifier.Format(id, ".bookmark");
    }

    BookmarkName(ulong id, string name)
    {
        Id = id;
        _name = name;
    }

    public static bool TryParse(string name, [NotNullWhen(true)] out BookmarkName? parsed)
    {
        if (Identifier.TryParse(name, ".bookmark", out var id))
        {
            parsed = new BookmarkName(id.Value, name);
            return true;
        }

        parsed = null;
        return false;
    }

    public override string ToString()
    {
        return _name;
    }
}
