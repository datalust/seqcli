using System;
using System.Collections.Generic;
using System.Linq;
using SeqCli.Forwarder.Filesystem;

namespace SeqCli.Tests.Forwarder.Filesystem;

class InMemoryStoreDirectory : StoreDirectory
{
    readonly Dictionary<string, InMemoryStoreFile> _files = new();

    public IReadOnlyDictionary<string, InMemoryStoreFile> Files => _files;

    public override InMemoryStoreFile Create(string name)
    {
        if (_files.ContainsKey(name)) throw new Exception($"The file {name} already exists.");

        _files.Add(name, new InMemoryStoreFile());

        return _files[name];
    }

    public override InMemoryStoreFile Create(string name, Span<byte> contents)
    {
        var file = Create(name);
        file.Append(contents);

        return file;
    }

    public override bool TryDelete(string name)
    {
        var existing = _files[name];

        if (!existing.CanDelete())
        {
            throw new InvalidOperationException($"Cannot delete {name} with active handles");
        }
        
        return _files.Remove(name);
    }

    public override InMemoryStoreFile Replace(string destinationPath, string sourcePath)
    {
        _files[destinationPath] = _files[sourcePath];

        if (!TryDelete(sourcePath))
        {
            throw new InvalidOperationException($"Failed to replace {destinationPath} with {sourcePath}");
        }

        return _files[destinationPath];
    }

    public override IEnumerable<(string Name, StoreFile File)> List(Func<string, bool> predicate)
    {
        return _files
            .Where(kv => predicate(kv.Key))
            .Select(kv => (kv.Key, kv.Value as StoreFile));
    }
}
