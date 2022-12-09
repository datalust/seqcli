using System.ComponentModel;
using System.IO;
using Seq.Apps;
using SeqCli.Apps.Definitions;
using Serilog.Events;
using Xunit;

// ReSharper disable all

namespace SeqCli.Tests.Apps;

public class AppDefinitionFormatterTests
{
    enum DomesticAnimal
    {
        Cat,
        Dog,
        Goldfish,
        [Description("Exotic species")]
        ExoticSpecies
    }
        
    [SeqApp("Example App", Description = "Just for this test!")]
    class ExampleApp : SeqApp, ISubscribeTo<LogEvent>
    {
        [SeqAppSetting(DisplayName = "Pet name")]
        public string PetName { get; set; }
            
        [SeqAppSetting(HelpText = "The species of your pet.", IsOptional = true)]
        public DomesticAnimal? Species { get; set; }

        public void On(Event<LogEvent> evt)
        {
        }
    }
        
    [Fact]
    public void FormatsDefinitions()
    {
        var expected = File.ReadAllText("Apps/ExampleApp.d.json");
        var formatted = new StringWriter();
        AppDefinitionFormatter.FormatAppDefinition(typeof(ExampleApp), true, formatted);
        Assert.Equal(Normalize(expected), Normalize(formatted.ToString()));
    }

    static string Normalize(string s)
    {
        return s.Trim().Replace("\r", "");
    }
}