#nullable enable
using System.Threading.Tasks;
using SeqCli.Ingestion;
using SeqCli.Tests.Support;
using Xunit;

namespace SeqCli.Tests.PlainText;

public class StaticMessageTemplateReaderTests
{
    [Fact]
    public async Task ReaderSubstitutesMessageTemplate()
    {
        var evt = Some.EventJson();
        evt["@m"] = "A pre-rendered message";
        const string mt = "This is a message template";
        var reader = new FixedEventReader(new ReadResult(evt, false));
        var wrapper = new StaticMessageTemplateReader(reader, mt);
        var result = await wrapper.TryReadAsync();
        Assert.Equal(mt, (string?)result.Document!["@mt"]);
        Assert.False(result.Document.ContainsKey("@m"));
    }
}
