using System;

namespace SeqCli.Cli.Features;

class ConfirmFeature: CommandFeature
{
    bool Yes { get; set; }
        
    public override void Enable(OptionSet options)
    {
        options.Add("y|confirm",
            "Answer [y]es when prompted to continue",
            _ => Yes = true);
    }

    public bool TryConfirm(string prompt)
    {
        if (Yes)
        {
            return true;
        }
            
        Console.Error.WriteLine($"{prompt} Continue?");
        Console.Error.Write("[y/N]: ");
        var k = Console.ReadKey();
        Console.Error.WriteLine();
        return k.Key == ConsoleKey.Y && (k.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) == 0;
    }
}