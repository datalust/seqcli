// Copyright © Datalust and contributors.
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
using System.IO;
using System.Threading.Tasks;
using SeqCli.Skills;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.Skills;

[Command("skills", "install", "Install or update Seq agent skills",
    Example = "seqcli skills install --global --agent claude")]
class InstallCommand : Command
{
    bool _global;
    string? _agent;
    
    public InstallCommand()
    {
        Options.Add(
            "g|global",
            "Install skills globally, to `~/.{agent}/skills`; the default is to install locally, in `./{agent}/skills.`",
            _ => _global = true);
        
        Options.Add(
            "a=|agent=",
            "The agent name to install skills for; the default is the generic name `agents`.",
            t => _agent = ArgumentString.Normalize(t));
    }

    protected override Task<int> Run()
    {
        var skillsPath = Path.Combine(
            _global ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : Environment.CurrentDirectory,
            $".{_agent?.ToLowerInvariant() ?? "agents"}",
            "skills");
        
        Log.Information("Installing skills to {SkillsPath}", skillsPath);
        SkillInstaller.Install(skillsPath);

        return Task.FromResult(0);
    }
}