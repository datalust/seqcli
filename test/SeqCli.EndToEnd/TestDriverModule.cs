using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Module = Autofac.Module;

namespace SeqCli.EndToEnd;

class TestDriverModule : Module
{
    readonly Args _args;

    public TestDriverModule(Args args)
    {
        _args = args;
    }
        
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(_args);
            
        // This enables running the program with an argument like `*Ingest*` to match all test cases
        // with `Ingest` in their names.
        var testCases = _args.TestCases();
        builder.RegisterAssemblyTypes(ThisAssembly)
            // ReSharper disable once AssignNullToNotNullAttribute
            .Where(t => testCases.Length == 0 || testCases.Any(c => c.IsMatch(t.FullName)))
            .As<ICliTestCase>()
            .WithMetadata(t =>
            {
                // Autofac doesn't appear to allow optional metadata using the short-cut method.
                var a = t.GetCustomAttribute<CliTestCaseAttribute>();
                var m = new Dictionary<string, object>();
                if (a != null)
                {
                    m[nameof(a.Multiuser)] = a.Multiuser;
                    m[nameof(a.MinimumApiVersion)] = a.MinimumApiVersion;
                }

                return m;
            });

        builder.RegisterType<LicenseSetup>().InstancePerOwned<IsolatedTestCase>();

        builder.RegisterType<TestConfiguration>().InstancePerOwned<IsolatedTestCase>();
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
            
        builder.RegisterSource(new IsolatedTestCaseRegistrationSource());
    }
}