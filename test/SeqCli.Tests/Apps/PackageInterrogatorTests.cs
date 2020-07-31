using System.IO;
using Newtonsoft.Json;
using SeqCli.Apps.Definitions;
using Xunit;

namespace SeqCli.Tests.Apps
{
    public class PackageInterrogatorTests
    {
        [Fact]
        public void FindsAppConfiguration()
        {
            var appBinaries = "Apps/FirstOfTypeBinaries";
            Assert.True(Directory.Exists(appBinaries));

            var configuration = PackageInterrogator.FindAppConfiguration(appBinaries, null, false);
            Assert.NotNull(configuration);

            var definition = JsonConvert.DeserializeObject<AppDefinition>(configuration);
            
            Assert.Equal("First of Type", definition.Name);
            
            var typeName = definition.Platform["hosted-dotnet"].SeqAppTypeName;
            Assert.Equal("Seq.App.FirstOfType.FirstOfTypeDetector", typeName);
        }
    }
}