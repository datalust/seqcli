﻿// Copyright 2018-2021 Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SeqCli.Config;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands;

[Command("config", "View and set fields in the `SeqCli.json` file; run with no arguments to list all fields")]
class ConfigCommand : Command
{
    string? _key, _value;
    bool _clear;

    public ConfigCommand()
    {
        Options.Add("k|key=", "The field, for example `connection.serverUrl`", k => _key = k);
        Options.Add("v|value=", "The field value; if not specified, the command will print the current value", v => _value = v);
        Options.Add("c|clear", "Clear the field", _ => _clear = true);
    }

    protected override Task<int> Run()
    {
        var verb = "read";
            
        try
        {
            var config = SeqCliConfig.ReadFromFile(RuntimeConfigurationLoader.DefaultConfigFilename);
            
            if (_key != null)
            {
                if (_clear)
                {
                    verb = "clear";
                    KeyValueSettings.Clear(config, _key);
                    SeqCliConfig.Write(config, RuntimeConfigurationLoader.DefaultConfigFilename);
                }
                else if (_value != null)
                {
                    verb = "update";
                    KeyValueSettings.Set(config, _key, _value);
                    SeqCliConfig.Write(config, RuntimeConfigurationLoader.DefaultConfigFilename);
                }
                else
                {
                    verb = "print";
                    Print(config, _key);
                }
            }
            else
            {
                List(config);
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not {Verb} config: {ErrorMessage}", verb, Presentation.FormattedMessage(ex));
            return Task.FromResult(1);
        }
    }
    
    static void Print(SeqCliConfig config, string key)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (key == null) throw new ArgumentNullException(nameof(key));

        if (!KeyValueSettings.TryGetValue(config, key, out var value, out _))
            throw new ArgumentException($"Option {key} not found");
            
        Console.WriteLine(value);
    }

    static void List(SeqCliConfig config)
    {
        foreach (var (key, value, _) in KeyValueSettings.Inspect(config))
        {
            Console.WriteLine($"{key}={value}");
        }
    }
}