using System;
using SeqCli.Output;
using Xunit;

namespace SeqCli.Tests.Output;

public class PrintTemplateTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData(@"{@m}\\{@x}", @"{@m}\{@x}")]
    [InlineData(@"{@m}\n{@x}", "{@m}\n{@x}")]
    [InlineData(@"{@m}\r\n{@x}", "{@m}\r\n{@x}")]
    [InlineData(@"\\\n", "\\\n")]
    public void PrintTemplatesAreProcessedCorrectly(string template, string expected)
    {
        var actual = PrintTemplate.InterpretEscapeChars(template);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(@"\")]
    [InlineData(@"test\")]
    [InlineData(@"\q")]
    public void InvalidTemplatesAreRejected(string template)
    {
        Assert.Throws<ArgumentException>(() => PrintTemplate.InterpretEscapeChars(template));
    }
}