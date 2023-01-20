using SeqCli.Apps.Hosting;
using Xunit;

namespace SeqCli.Tests.Apps;

public class AppActivatorTests
{
    enum Test
    {
        First,
        Second
    }
        
    [Fact]
    public void CanConvertStringsToEnumValues()
    {
        var converted = AppActivator.ConvertToSettingType("Second", typeof(Test));
        Assert.Equal(Test.Second, converted);
    }
}