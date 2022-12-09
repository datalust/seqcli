using System.Threading.Tasks;
using Autofac;
using SeqCli.EndToEnd.Support;

namespace SeqCli.EndToEnd;

static class Program
{
    static async Task<int> Main(string[] rawArgs)
    {
        var args = new Args(rawArgs);

        var builder = new ContainerBuilder();
        builder.RegisterModule(new TestDriverModule(args));
        await using var container = builder.Build();
        return await container.Resolve<TestDriver>().Run();
    }
}