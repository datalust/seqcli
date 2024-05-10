using SeqCli.Forwarder.Storage;
using Xunit;

namespace SeqCli.Tests.Forwarder.Storage;

public class IdentifierTests
{
    [Theory]
    [InlineData("0000000000000000.clef", 0)]
    [InlineData("0000000000000001.clef", 1)]
    [InlineData("000000000000000a.clef", 10)]
    [InlineData("ffffffffffffffff.clef", ulong.MaxValue)]
    public void ParseValid(string name, ulong expected)
    {
        Assert.True(ChunkName.TryParse(name, out var actual));

        Assert.Equal(expected, actual.Value.Id);
        Assert.Equal(name, actual.Value.ToString());
    }

    [Theory]
    [InlineData("0.clef")]
    [InlineData("one.clef")]
    [InlineData("00000000000.clef.value")]
    [InlineData("0ffffffffffffffff.clef")]
    [InlineData("0xffffffffffffff.clef")]
    public void ParseInvalid(string name)
    {
        Assert.False(ChunkName.TryParse(name, out _));
    }
}
