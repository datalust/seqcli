using System.Threading.Tasks;
using Autofac;
using SeqCli.Cli;
using Serilog;

namespace SeqCli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<SeqCliModule>();

            using (var container = builder.Build())
            {
                var clh = container.Resolve<CommandLineHost>();
                var exit = await clh.Run(args);
                Log.CloseAndFlush();
                return exit;
            }
        }
    }
}