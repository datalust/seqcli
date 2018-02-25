using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SeqCli.Cli;
using Xunit;

namespace SeqCli.Tests.Cli
{

    public class NoDuplicateCommands
    {
        [Fact]
        public void EnsureNoDuplicateCommands()
        {
            var anyDuplicates = typeof(Command).GetTypeInfo().Assembly.GetExportedTypes()
                .Where(t => t.IsAssignableFrom(typeof(Command)))
                .Select(t => new {CommandType = t, Attribute = t.GetCustomAttribute<CommandAttribute>()})
                .GroupBy(t => new {t.Attribute.Name, t.Attribute.SubCommand})
                .Any(t => t.Count() > 1);

            Assert.Equal(anyDuplicates, false);
        }
    }
}
