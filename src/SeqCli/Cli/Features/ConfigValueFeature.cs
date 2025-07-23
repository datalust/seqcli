using System;

namespace SeqCli.Cli.Features;

class ConfigValueFeature: CommandFeature
{
    // An empty string is normalized to null/unset, which will normally be considered "cleared"; we allow this
    // to keep the CLI backwards-compatible.
    bool _valueSpecified;
        
    string? Value { get; set; }
    bool ReadValueFromStdin { get; set; }

    public override void Enable(OptionSet options)
    {
        options.Add("v|value=",
            "The field value, comma-separated if multiple values are accepted",
            v =>
            {
                _valueSpecified = true;
                // Not normalized; some settings might include leading/trailing whitespace.
                Value = v;
            });

        options.Add("value-stdin",
            "Read the value from `STDIN`",
            _ => ReadValueFromStdin = true);
    }

    public string? ReadValue()
    {
        if (!_valueSpecified && !ReadValueFromStdin)
            throw new ArgumentException(
                "A value must be supplied with either `--value=VALUE` or `--value-stdin`.");
        
        return ReadValueFromStdin ? Console.In.ReadToEnd().TrimEnd('\r', '\n') : Value;
    }
}
