using System;
using Autofac;
using SeqCli.EndToEnd;
using SeqCli.EndToEnd.Support;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

try
{
    var testDriverArgs = new Args(args);

    var builder = new ContainerBuilder();
    builder.RegisterModule(new TestDriverModule(testDriverArgs));
    await using var container = builder.Build();
    return await container.Resolve<TestDriver>().Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Testing failed");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
