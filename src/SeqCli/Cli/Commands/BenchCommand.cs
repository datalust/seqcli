#nullable enable
using System;
using System.Threading.Tasks;
using SeqCli.Cli.Features;

namespace SeqCli.Cli.Commands;

[Command("bench", "Measure query performance")]
class BenchCommand : Command
{
    int _runs = 0;
    readonly ConnectionFeature _connection;
    readonly DateRangeFeature _range;
    string _cases = "";

    const int DefaultRuns = 3;

    public BenchCommand()
    {
        Options.Add("r|runs=", "The number of runs to execute", r =>
        {
            if (!int.TryParse(r, out _runs))
            {
                _runs = DefaultRuns;
            }
        });
        
        Options.Add("c|cases=", "A JSON file containing the set of cases to run. Defaults to a standard set of cases", c => _cases = c);
        
        _connection = Enable<ConnectionFeature>();
        _range = Enable<DateRangeFeature>();
    }
    
    protected override Task<int> Run()
    {
        Console.WriteLine("Bench command here");
        return Task.FromResult(0);
    }
}