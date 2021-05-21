using System;
using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Templates
{
    public class TemplateExportImportTestCase : ICliTestCase
    {
        readonly TestDataFolder _testDataFolder;

        public TemplateExportImportTestCase(TestDataFolder testDataFolder)
        {
            _testDataFolder = testDataFolder;
        }
        
        public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
        {
            var originalTitle = Guid.NewGuid().ToString("n");
            var description = Guid.NewGuid().ToString("n");
            
            var signal = await connection.Signals.TemplateAsync();
            signal.OwnerId = null;
            signal.Title = originalTitle;
            signal.Description = description;

            await connection.Signals.AddAsync(signal);

            var exit = runner.Exec("template export", $"-o \"{_testDataFolder.Path}\"");
            Assert.Equal(0, exit);

            var exportedFilename = _testDataFolder.ItemPath($"signal-{originalTitle}.template");
            Assert.True(File.Exists(exportedFilename));

            var content = await File.ReadAllTextAsync(exportedFilename);
            
            var newTitle = Guid.NewGuid().ToString("n");
            content = content.Replace(originalTitle, newTitle);

            await File.WriteAllTextAsync(exportedFilename, content);

            exit = runner.Exec("template import", $"-i \"{_testDataFolder.Path}\"");
            Assert.Equal(0, exit);

            var created = Assert.Single(await connection.Signals.ListAsync(shared: true), s => s.Title == newTitle)!;
            Assert.Equal(description, created!.Description);
            
            // Uses import state
            exit = runner.Exec("template import", $"-i \"{_testDataFolder.Path}\"");
            Assert.Equal(0, exit);

            var updated = Assert.Single(await connection.Signals.ListAsync(shared: true), s => s.Title == newTitle)!;
            Assert.Equal(created.Id, updated.Id);
        }
    }
}
