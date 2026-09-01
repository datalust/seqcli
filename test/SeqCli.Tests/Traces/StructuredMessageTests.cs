#nullable enable
using System.IO;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using SeqCli.Traces;
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
            Assert.Equal("", message);
            Assert.Empty(properties);
        }
    }

    [Fact]
    public void TextTokensAreRead()
    {
        var (message, properties) = StructuredMessage.Read(new JArray("Hello", ", ", "world"));

        Assert.Equal("Hello, world", message);
        Assert.Empty(properties);
    }

    [Fact]
    public void LiteralBracesAreEscapedInTemplateText()
    {
        var (message, _) = StructuredMessage.Read(new JArray("a {not-a-hole} b"));

        Assert.Equal("a {{not-a-hole}} b", message);
    }

    [Fact]
    public void HolesCarryRawTextAndValues()
    {
        var (message, properties) = StructuredMessage.Read(new JArray(
            "Hello, ", Hole("Name", "{Name:x}", "World"), "!"));

        Assert.Equal("Hello, {Name:x}!", message);

        var property = Assert.Single(properties);
        Assert.Equal("Name", property.Key);
        Assert.Equal("World", (string?)property.Value);
    }

    [Fact]
    public void HolesWithoutValuesContributeNoProperties()
    {
        var (message, properties) = StructuredMessage.Read(new JArray(Hole("Name")));

        Assert.Equal("{Name}", message);
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
    public void ScalarHoleValuesAreRead()
    {
        var (_, properties) = StructuredMessage.Read(new JArray(Hole("Count", value: 42L)));

        Assert.Equal(42L, (long?)Assert.Single(properties).Value);
    }

    [Fact]
    public void StructuredHoleValuesBecomeObjects()
    {
        var (_, properties) = StructuredMessage.Read(new JArray(
            Hole("Order", value: new JObject(new JProperty("Id", 7)))));

        var structure = Assert.IsType<JsonObject>(Assert.Single(properties).Value);
        Assert.Equal(7, (int?)structure["Id"]);
    }

    [Fact]
    public void DottedHoleNamesBecomeNestedObjects()
    {
        var (message, properties) = StructuredMessage.Read(new JArray(
            Hole("user.name", value: "Barney")));

        Assert.Equal("{user.name}", message);
        var user = Assert.IsType<JsonObject>(properties["user"]);
        Assert.Equal("Barney", (string?)user["name"]);
    }

    [Fact]
    public void TrailingWhitespaceIsTrimmed()
    {
        var (message, _) = StructuredMessage.Read(new JArray("Hi ", "}", " \n"));

        Assert.Equal("Hi }}", message);
    }

    [Fact]
    public void WhitespaceOnlyMessagesReadAsEmpty()
    {
        var (message, _) = StructuredMessage.Read(new JArray("   "));

        Assert.Equal("", message);
    }

    [Fact]
    public void TrailingHolesAreNotTrimmed()
    {
        var (message, _) = StructuredMessage.Read(new JArray("Took ", Hole("Elapsed")));

        Assert.Equal("Took {Elapsed}", message);
    }

    [Fact]
    public void UnexpectedCellsAndTokensAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => StructuredMessage.Read("just text"));
        Assert.Throws<InvalidDataException>(() => StructuredMessage.Read(new JArray(42)));
        Assert.Throws<InvalidDataException>(() => StructuredMessage.Read(new JArray(new JObject())));
    }
}
