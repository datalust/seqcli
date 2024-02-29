using System.IO;
using System.Linq;
using SeqCli.Config;
using SeqCli.Forwarder.Cryptography;
using SeqCli.Forwarder.Multiplexing;
using SeqCli.Tests.Support;
using Xunit;

namespace SeqCli.Tests.Forwarder.Multiplexing;

public class ActiveLogBufferMapTests
{
    [Fact]
    public void AnEmptyMapCreatesNoFiles()
    {
        using var tmp = new TempFolder("Buffer");
        using var map = CreateActiveLogBufferMap(tmp);
        Assert.Empty(Directory.GetFileSystemEntries(tmp.Path));
    }

    [Fact]
    public void TheDefaultBufferWritesDataInTheBufferRoot()
    {
        using var tmp = new TempFolder("Buffer");
        using var map = CreateActiveLogBufferMap(tmp);
        var entry = map.GetLogBuffer(null);
        Assert.NotNull(entry);
        Assert.True(File.Exists(Path.Combine(tmp.Path, "data.mdb")));
        Assert.Empty(Directory.GetDirectories(tmp.Path));
        Assert.Same(entry, map.GetLogBuffer(null));
    }

    [Fact]
    public void ApiKeySpecificBuffersWriteDataToSubfolders()
    {
        using var tmp = new TempFolder("Buffer");
        using var map = CreateActiveLogBufferMap(tmp);
        string key1 = Some.ApiKey(), key2 = Some.ApiKey();
        var entry1 = map.GetLogBuffer(key1);
        var entry2 = map.GetLogBuffer(key2);

        Assert.NotNull(entry1);
        Assert.NotNull(entry2);
        Assert.Same(entry1, map.GetLogBuffer(key1));
        Assert.NotSame(entry1, entry2);
        var subdirs = Directory.GetDirectories(tmp.Path);
        Assert.Equal(2, subdirs.Length);
        Assert.True(File.Exists(Path.Combine(subdirs[0], "data.mdb")));
        Assert.True(File.Exists(Path.Combine(subdirs[0], ".apikey")));
    }

    [Fact]
    public void EntriesSurviveReloads()
    {
        var apiKey = Some.ApiKey();
        var value = Some.Bytes(100);

        using var tmp = new TempFolder("Buffer");
        using (var map = CreateActiveLogBufferMap(tmp))
        {
            map.GetLogBuffer(null).Enqueue([value]);
            map.GetLogBuffer(apiKey).Enqueue([value]);
        }

        using (var map = CreateActiveLogBufferMap(tmp))
        {
            var first = map.GetLogBuffer(null).Peek(0).Single();
            var second = map.GetLogBuffer(apiKey).Peek(0).Single();
            Assert.Equal(value, first.Value);
            Assert.Equal(value, second.Value);
        }
    }

    static ActiveLogBufferMap CreateActiveLogBufferMap(TempFolder tmp)
    {
        var config = new SeqCliConfig();
        var map = new ActiveLogBufferMap(tmp.Path, config.Forwarder.Storage, config.Connection, new InertLogShipperFactory(), StringDataProtector.CreatePlatformDefault());
        map.Load();
        return map;
    }
}