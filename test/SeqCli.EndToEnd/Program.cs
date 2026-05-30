using System;
using Autofac;
using SeqCli.EndToEnd;
using SeqCli.EndToEnd.Support;
using Serilog;
using Serilog.Debugging;

// Test cases reference data files using paths relative to the working directory (e.g. `Data/log-*.txt`),
// which are copied alongside the test binary. Anchor the working directory to the build output so the
// suite behaves identically however it's launched - `dotnet run --project ...` from the repo root, or
// `cd`-ing into the test directory first as CI does.
Environment.CurrentDirectory = AppContext.BaseDirectory;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

SelfLog.Enable(Console.Error);

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
