using System.IO;
using SeqCli.Apps;
using SeqCli.Apps.Definitions;
using Xunit;

namespace SeqCli.Tests.Apps
{
    public class AppLoaderTests
    {
        [Fact]
        public void FindsAppConfiguration()
        {
            const string appBinaries = "Apps/FirstOfTypeBinaries";
            Assert.True(Directory.Exists(appBinaries));

            using var loader = new AppLoader(appBinaries);
            Assert.True(loader.TryLoadSeqAppType(null, out var seqAppType));

            var definition = AppMetadataReader.ReadFromSeqAppType(seqAppType);
            
            Assert.Equal("First of Type", definition.Name);
            
            var typeName = definition.Platform["hosted-dotnet"].SeqAppTypeName;
            Assert.Equal("Seq.App.FirstOfType.FirstOfTypeDetector", typeName);
        }
    }
}