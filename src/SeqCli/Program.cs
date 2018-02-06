using System;
using System.Threading.Tasks;
using Autofac;
using SeqCli.Cli;
using Serilog;
using Serilog.Events;

namespace SeqCli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.Console(
                    outputTemplate: "{Message:lj}{NewLine}",
                    standardErrorFromLevel: LevelAlias.Minimum)
                .CreateLogger();
            
            try
            {
                TaskScheduler.UnobservedTaskException += 
                    (s,e) => Log.Error(e.Exception, "Unobserved task exception: {UnobservedExceptionMessage}");
                
                var builder = new ContainerBuilder();
                builder.RegisterModule<SeqCliModule>();

                using (var container = builder.Build())
                {
                    var clh = container.Resolve<CommandLineHost>();
                    var exit = await clh.Run(args);
                    return exit;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "The command failed: {UnhandledExceptionMessage}", ex.Message);
                return -1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}