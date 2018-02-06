using Autofac;
using SeqCli.Cli;
using Serilog;

namespace SeqCli
{
    class Program
    {
        static int Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<SeqCliModule>();

            using (var container = builder.Build())
            {
                var clh = container.Resolve<CommandLineHost>();
                var exit = clh.Run(args);
                Log.CloseAndFlush();
                return exit;
            }
        }
    }
}