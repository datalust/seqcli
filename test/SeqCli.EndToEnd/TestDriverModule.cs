using System;
using System.Linq;
using Autofac;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;

namespace SeqCli.EndToEnd
{
    class TestDriverModule : Module
    {
        readonly Args _args;

        public TestDriverModule(Args args)
        {
            _args = args;
        }
        
        protected override void Load(ContainerBuilder builder)
        {
            // This enables running the program with an argument like `*Ingest*` to match all test cases
            // with `Ingest` in their names.
            var testCases = _args.TestCases();
            builder.RegisterAssemblyTypes(ThisAssembly)
                .Where(t => testCases == null || testCases.Length == 0 || testCases.Any(c => c.IsMatch(t.FullName)))
                .As<ICliTestCase>();

            builder.RegisterType<TestConfiguration>().SingleInstance();
            builder.RegisterType<TestDataFolder>().InstancePerOwned<IsolatedTestCase>();
            builder.RegisterType<TestDriver>();
            builder.RegisterType<CliCommandRunner>();

            builder.Register(c =>
                {
                    var configuration = c.Resolve<TestConfiguration>();
                    return configuration.SpawnServerProcess(c.Resolve<TestDataFolder>().Path);
                })
                .As<ITestProcess>()
                .InstancePerOwned<IsolatedTestCase>();

            builder.Register(c => new SeqConnection(c.Resolve<TestConfiguration>().ServerListenUrl))
                .InstancePerOwned<IsolatedTestCase>();

            builder.Register(c => new LoggerConfiguration()
                    .AuditTo.Seq(c.Resolve<SeqConnection>().Client.ServerUrl)
                    .CreateLogger())
                .As<ILogger>()
                .InstancePerOwned<IsolatedTestCase>();

            builder.RegisterAdapter<ICliTestCase, IsolatedTestCase>((ctx, tc) =>
                new IsolatedTestCase(
                    ctx.Resolve<Lazy<ITestProcess>>(),
                    ctx.Resolve<Lazy<SeqConnection>>(),
                    ctx.Resolve<Lazy<ILogger>>(),
                    ctx.Resolve<CliCommandRunner>(),
                    tc));
        }
    }
}
