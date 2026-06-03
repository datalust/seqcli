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
using System.Collections.Generic;
using System.IO;
using Serilog;

namespace SeqCli.Skills;

static class SkillInstaller
{
    static readonly IReadOnlyDictionary<string, SkillTarget> KnownAgents =
        new Dictionary<string, SkillTarget>
        {
            ["copilot"] = new(global => global
                ? Path.Combine(UserProfile, ".copilot", "skills")
                : Path.Combine(Environment.CurrentDirectory, ".github", "skills")),
        };

    static readonly IReadOnlyDictionary<string, string> AgentAliases =
        new Dictionary<string, string>
        {
            ["goose"] = "agents",
            ["github"] = "copilot",
            ["codex"] = "agents"
        };

    public static void Install(string? agent, bool global)
    {
        agent ??= "agents";

        if (AgentAliases.TryGetValue(agent, out var alias))
            agent = alias;

        var target = KnownAgents.TryGetValue(agent, out var known) ? known : Convention(agent);
        var destinationPath = target.ResolveSkillsDirectory(global);

        Log.Information("Installing skills to {SkillsPath}", destinationPath);

        var sourcePath = Path.Combine(AppContext.BaseDirectory, "Skills");

        foreach (var skillSourceDirectory in Directory.EnumerateDirectories(sourcePath))
        {
            var skillName = Path.GetFileName(skillSourceDirectory);
            var destination = Path.Combine(destinationPath, skillName);

            Console.Write("Installing skill `{0}` to `{1}`...", skillName, destinationPath);

            CopyFilesRecursive(skillSourceDirectory, destination);
            
            Console.WriteLine(" Done.");
        }
    }

    static SkillTarget Convention(string agent) =>
        new(global => Path.Combine(
            global ? UserProfile : Environment.CurrentDirectory,
            $".{agent}",
            "skills"));

    static string UserProfile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    static void CopyFilesRecursive(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        
        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            CopyFilesRecursive(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    sealed record SkillTarget(Func<bool, string> ResolveSkillsDirectory);
}