#nullable enable
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SeqCli.Traces;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace SeqCli.Tests.Traces;

public class StructuredMessageTests
{
    static JObject Hole(string name, string? raw = null, object? value = null)
    {
        var hole = new JObject(new JProperty("name", name), new JProperty("raw", raw ?? $"{{{name}}}"));
        if (value != null)
            hole.Add("value", JToken.FromObject(value));
        return hole;
    }

    [Fact]
    public void MissingStructuredMessagesReadAsEmpty()
    {
        foreach (var cell in new object?[] { null, JValue.CreateNull() })
        {
            var (message, properties) = StructuredMessage.Read(cell);
            Assert.Empty(message.Tokens);
            Assert.Empty(properties);
        }
    }

    [Fact]
    public void TextTokensAreRead()
    {
        var (message, properties) = StructuredMessage.Read(new JArray("Hello", ", ", "world"));

        Assert.Equal("Hello, world", message.Text);
        Assert.All(message.Tokens, token => Assert.IsType<TextToken>(token));
        Assert.Empty(properties);
    }

    [Fact]
    public void HolesCarryRawTextAndValues()
    {
        var (message, properties) = StructuredMessage.Read(new JArray(
            "Hello, ", Hole("Name", "{Name:x}", "World"), "!"));

        Assert.Equal("Hello, {Name:x}!", message.Text);
        var hole = Assert.IsType<PropertyToken>(message.Tokens.ElementAt(1));
        Assert.Equal("Name", hole.PropertyName);

        var property = Assert.Single(properties);
        Assert.Equal("Name", property.Name);
        Assert.Equal(new ScalarValue("World"), property.Value);
    }

    [Fact]
    public void HolesWithoutValuesContributeNoProperties()
    {
        var (message, properties) = StructuredMessage.Read(new JArray(Hole("Name")));

        Assert.Equal("{Name}", message.Text);
        Assert.Empty(properties);
    }

    [Fact]
    public void DuplicateHolesContributeASingleProperty()
    {
        var (_, properties) = StructuredMessage.Read(new JArray(
            Hole("Name", value: "World"), " and ", Hole("Name", value: "World")));

        Assert.Single(properties);
    }

    [Fact]
    public void ScalarHoleValuesAreUnwrapped()
    {
        var (_, properties) = StructuredMessage.Read(new JArray(Hole("Count", value: 42L)));

        var scalar = Assert.IsType<ScalarValue>(Assert.Single(properties).Value);
        Assert.Equal(42L, scalar.Value);
    }

    [Fact]
    public void StructuredHoleValuesBecomeStructures()
    {
        var (_, properties) = StructuredMessage.Read(new JArray(
            Hole("Order", value: new JObject(new JProperty("Id", 7)))));

        var structure = Assert.IsType<StructureValue>(Assert.Single(properties).Value);
        Assert.Equal("Id", Assert.Single(structure.Properties).Name);
    }

    [Fact]
    public void TrailingWhitespaceIsTrimmed()
    {
        var (message, _) = StructuredMessage.Read(new JArray("Hi ", "}", " \n"));

        Assert.Equal("Hi }", message.Text);
    }

    [Fact]
    public void WhitespaceOnlyMessagesReadAsEmpty()
    {
        var (message, _) = StructuredMessage.Read(new JArray("   "));

        Assert.Empty(message.Tokens);
    }

    [Fact]
    public void TrailingHolesAreNotTrimmed()
    {
        var (message, _) = StructuredMessage.Read(new JArray("Took ", Hole("Elapsed")));

        Assert.Equal("Took {Elapsed}", message.Text);
    }

    [Fact]
    public void UnexpectedCellsAndTokensAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => StructuredMessage.Read("just text"));
        Assert.Throws<InvalidDataException>(() => StructuredMessage.Read(new JArray(42)));
        Assert.Throws<InvalidDataException>(() => StructuredMessage.Read(new JArray(new JObject())));
    }
}
