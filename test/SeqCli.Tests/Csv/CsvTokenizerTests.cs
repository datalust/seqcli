using System.Collections.Generic;
using System.Linq;
using SeqCli.Csv;
using Superpower.Model;
using Xunit;

namespace SeqCli.Tests.Csv;

public class CsvTokenizerTests
{
    [Fact]
    public void AnEmptyStringYieldsNoTokens()
    {
        Assert.Empty(Tokenize(""));
    }

    [Fact]
    public void TokenizesATextCell()
    {
        var tokens = Tokenize("\"abc\"");
        Assert.Equal(3, tokens.Count());
        Assert.Equal(tokens.Select(t => t.Kind), new[]{CsvToken.DoubleQuote, CsvToken.Text, CsvToken.DoubleQuote});
    }

    [Fact]
    public void TokenizesARow()
    {
        var tokens = Tokenize("\"abc\",\"def\"\r\n");
        Assert.Equal(8, tokens.Count());
    }

    [Theory]
    [InlineData(CsvToken.Text, "abc")]
    [InlineData(CsvToken.Text, "1a")]
    [InlineData(CsvToken.Text, "1\"\"")]
    [InlineData(CsvToken.Number, "1")]
    [InlineData(CsvToken.Number, "-123.45")]
    [InlineData(CsvToken.Number, "0xa123")]
    [InlineData(CsvToken.Boolean, "true")]
    [InlineData(CsvToken.Boolean, "false")]
    [InlineData(CsvToken.Null, "null")]
    public void DetectsSpecialTokenTypes(object o, string cell)
    {
        var tokenKind = (CsvToken) o;
        var content = Tokenize($"\"{cell}\"").ElementAt(1);
        Assert.Equal(tokenKind, content.Kind);
    }

    static TokenList<CsvToken> Tokenize(string csv)
    {
        return new CsvTokenizer().Tokenize(csv);
    }
}