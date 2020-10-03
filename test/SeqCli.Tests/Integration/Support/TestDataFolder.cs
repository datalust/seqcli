using System;
using System.IO;

namespace SeqCli.Tests.Integration.Support
{
    sealed class TestDataFolder : IDisposable
    {
        readonly string _basePath;

        public TestDataFolder()
        {
            _basePath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "Seq Test",
                Guid.NewGuid().ToString("n"));

            Directory.CreateDirectory(_basePath);
        }

        public string Path => _basePath;

        public string Folder(string relativePath)
        {
            return System.IO.Path.Combine(_basePath, relativePath);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_basePath, true);
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Failed to delete temporary path `{0}`.", Path);
                Console.ResetColor();
            }
        }
    }
}
