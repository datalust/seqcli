using SeqCli.Templates.Ast;
using SeqCli.Templates.Parser;
using Xunit;

namespace SeqCli.Tests.Templates;

public class JsonTemplateParserTests
{
    [Fact]
    public void TemplatesAreParsed()
    {
        var template = "{'a':[true, false, null, 12.3], 'b': test(42)}".Replace("'", "\"");
        Assert.True(JsonTemplateParser.TryParse(template, out var root, out _, out _));

        var obj = Assert.IsType<JsonTemplateObject>(root);
        var arr = Assert.IsType<JsonTemplateArray>(obj.Members["a"]);
        var t = Assert.IsType<JsonTemplateBoolean>(arr.Elements[0]);
        Assert.True(t.Value);
        var f = Assert.IsType<JsonTemplateBoolean>(arr.Elements[1]);
        Assert.False(f.Value);
        Assert.IsType<JsonTemplateNull>(arr.Elements[2]);
        var n = Assert.IsType<JsonTemplateNumber>(arr.Elements[3]);
        Assert.Equal(12.3m, n.Value);
        var call = Assert.IsType<JsonTemplateCall>(obj.Members["b"]);
        Assert.Equal("test", call.Name);
        Assert.IsType<JsonTemplateNumber>(call.Arguments[0]);
    }
}