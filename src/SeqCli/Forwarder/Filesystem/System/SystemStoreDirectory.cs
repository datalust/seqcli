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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SeqCli.Config;
using Serilog;

#if UNIX
using SeqCli.Forwarder.Filesystem.System.Unix;
#endif

namespace SeqCli.Forwarder.Filesystem.System;

sealed class SystemStoreDirectory : StoreDirectory
{
    readonly string _directoryPath;

    public SystemStoreDirectory(string path)
    {
        _directoryPath = Path.GetFullPath(path);

        if (!Directory.Exists(_directoryPath)) Directory.CreateDirectory(_directoryPath);
    }

    public void WriteApiKey(SeqCliConfig config, string apiKey)
    {
        File.WriteAllBytes(
            Path.Combine(_directoryPath, "api.key"), 
            config.Encryption.DataProtector().Encrypt(Encoding.UTF8.GetBytes(apiKey)));
    }

    public bool TryReadApiKey(SeqCliConfig config, [NotNullWhen(true)] out string? apiKey)
    {
        apiKey = null;
        var path = Path.Combine(_directoryPath, "api.key");

        if (!File.Exists(path)) return false;
        
        try
        {
            var encrypted = File.ReadAllBytes(path);
            apiKey = Encoding.UTF8.GetString(config.Encryption.DataProtector().Decrypt(encrypted));
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Could not read or decrypt api key");
        }
  
        return false;
    }

    public override SystemStoreFile Create(string name)
    {
        var filePath = Path.Combine(_directoryPath, name);
        using var _ = File.OpenHandle(filePath, FileMode.Create, FileAccess.ReadWrite,
            FileShare.ReadWrite | FileShare.Delete);
        Dirsync(_directoryPath);

        return new SystemStoreFile(filePath);
    }

    protected override (string, StoreFile) CreateTemporary()
    {
        // Temporary files are still created in the same directory
        // This is necessary for renames to be atomic on some filesystems
        var tmpName = $"rc{Guid.NewGuid():N}.tmp";

        var filePath = Path.Combine(_directoryPath, tmpName);
        using var _ = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.ReadWrite,
            FileShare.ReadWrite | FileShare.Delete, FileOptions.DeleteOnClose);

        return (tmpName, new SystemStoreFile(filePath));
    }

    public override bool TryDelete(string name)
    {
        var filePath = Path.Combine(_directoryPath, name);

        try
        {
            File.Delete(filePath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public override SystemStoreFile Replace(string toReplace, string replaceWith)
    {
        var filePath = Path.Combine(_directoryPath, toReplace);

        File.Replace(Path.Combine(_directoryPath, replaceWith), filePath, null);

        return new SystemStoreFile(filePath);
    }

    public override StoreFile ReplaceContents(string name, Span<byte> contents, bool sync = true)
    {
        var filePath = Path.Combine(_directoryPath, name);

        using var file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
            FileShare.ReadWrite | FileShare.Delete);

        // NOTE: This will be atomic if:
        // 1. The incoming contents are larger or equal in size to the length of the file
        // 2. The incoming contents are page sized or smaller
        file.Position = 0;
        file.Write(contents);

        if (sync) file.Flush(true);

        return new SystemStoreFile(filePath);
    }

    public override IEnumerable<(string Name, StoreFile File)> List(Func<string, bool> predicate)
    {
        foreach (var filePath in Directory.EnumerateFiles(_directoryPath))
        {
            var name = Path.GetFileName(filePath);

            if (!predicate(name)) continue;

            yield return (name, new SystemStoreFile(filePath));
        }
    }

    static void Dirsync(string directoryPath)
    {
#if UNIX
        var dir = Libc.open(directoryPath, 0);
        if (dir == -1) return;

        // NOTE: directory syncing here is best-effort
        // If it fails for any reason we simply carry on
#pragma warning disable CA1806
        Libc.fsync(dir);
        Libc.close(dir);
#pragma warning restore CA1806
#endif
    }
}
