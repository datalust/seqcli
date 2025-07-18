// Copyright 2019 Datalust Pty Ltd and Contributors
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
using System.Collections.Generic;
using System.Threading.Tasks;
using SeqCli.Apps.Hosting;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Util;

namespace SeqCli.Cli.Commands.App;

[Command("app", "run", "Host a .NET `[SeqApp]` plug-in",
    Example = "seqcli tail --json | seqcli app run -d \"./bin/Debug/netstandard2.2\" -p ToAddress=example@example.com")]
class RunCommand : Command
{
    bool _readEnv;

    string _dir = Environment.CurrentDirectory,
        _storage = Environment.CurrentDirectory,
        _serverUrl,
        _appInstanceTitle = "Test Instance",
        _appInstanceId = "appinstance-0";

    string?
        _type,
        _seqInstanceName;
        
    readonly Dictionary<string, string> _settings = new();

    public RunCommand()
    {
        // The usual `--storage` argument is not supported on this command (see notes on `--storage` arg below).
        _serverUrl = RuntimeConfigurationLoader.Load(new StoragePathFeature()).Connection.ServerUrl;

        Options.Add(
            "d=|directory=",
            "The directory containing .NET Standard assemblies; defaults to the current directory",
            d => _dir = ArgumentString.Normalize(d) ?? _dir);

        Options.Add(
            "type=",
            "The [SeqApp] plug-in type name; defaults to scanning assemblies for a single type marked with this attribute",
            t => _type = ArgumentString.Normalize(t));

        Options.Add(
            "p={=}|property={=}",
            "Specify name/value settings for the app, e.g. `-p ToAddress=example@example.com -p Subject=\"Alert!\"`",
            (n, v) =>
            {
                var name = n.Trim();
                var valueText = v?.Trim();
                _settings.Add(name, valueText ?? "");
            });
            
        // Important note, this conflicts with the `--storage` argument accepted by the majority of other commands; changing
        // this requires an update to Seq, which uses this command for hosting .NET apps.
        Options.Add(
            "storage=",
            "A directory in which app-specific data can be stored; defaults to the current directory",
            d => _storage = ArgumentString.Normalize(d) ?? _storage);
            
        Options.Add("s=|server=",
            "The URL of the Seq server, used only for app configuration (no connection is made to the server); by default the `connection.serverUrl` value will be used",
            v => _serverUrl = ArgumentString.Normalize(v) ?? _serverUrl);

        Options.Add(
            "server-instance=",
            "The instance name of the Seq server, used only for app configuration; defaults to no instance name",
            v => _seqInstanceName = ArgumentString.Normalize(v));

        Options.Add(
            "t=|title=",
            "The app instance title, used only for app configuration; defaults to a placeholder title.",
            v => _appInstanceTitle = ArgumentString.Normalize(v) ?? throw new ArgumentException("Title requires a value."));

        Options.Add(
            "id=",
            "The app instance id, used only for app configuration; defaults to a placeholder id.",
            v => _appInstanceId = ArgumentString.Normalize(v) ?? throw new ArgumentException("Id requires a value."));

        Options.Add(
            "read-env",
            "Read app configuration and settings from environment variables, as specified in " +
            "https://docs.datalust.co/docs/seq-apps-in-other-languages; ignores all options " +
            "except --directory and --type",
            _ => _readEnv = true);
    }

    protected override async Task<int> Run()
    {
        if (!_readEnv)
            return await AppHost.Run(_dir, _settings, _storage, _serverUrl, _appInstanceId, _appInstanceTitle, _seqInstanceName, _type);
            
        var fromEnv = AppEnvironment.ReadStandardEnvironment();
        return await AppHost.Run(_dir, fromEnv.Settings, fromEnv.StoragePath, fromEnv.ServerUrl, 
            fromEnv.AppInstanceId, fromEnv.AppInstanceTitle, fromEnv.SeqInstanceName, _type);
    }
}