using System;
using SeqCli.Util;

namespace SeqCli.Cli.Features;

class ConfigKeyFeature: CommandFeature
{
    string? _key;
    
    public override void Enable(OptionSet options)
    {
        options.Add("k|key=", "The field, for example `connection.serverUrl`", k => _key = ArgumentString.Normalize(k));
    }
        
    public string GetKey()
    {
        if (string.IsNullOrEmpty(_key))
            throw new ArgumentException("A field must be supplied with `--key=KEY`.");

        return _key;
    }
}
