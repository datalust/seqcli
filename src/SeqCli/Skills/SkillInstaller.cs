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
    // Agents whose skills directory diverges from the common `.{agent}/skills` convention.
    // Anything not listed here - including the default `agents` name, Claude Code, Gemini CLI,
    // Cursor, Junie, Kiro, and any unknown agent - uses the convention (see `Convention`), so
    // a conformant agent requires no change at all and a divergent one is a single entry here.
    static readonly IReadOnlyDictionary<string, SkillTarget> KnownAgents =
        new Dictionary<string, SkillTarget>
        {
            // Codex reads skills only from `.agents/skills` (repo) and `~/.agents/skills`
            // (user); it has no `.codex` skills dir, so route both scopes to the portable alias.
            ["codex"] = new(global => Path.Combine(
                global ? UserProfile : Environment.CurrentDirectory,
                ".agents",
                "skills")),

            // GitHub Copilot / VS Code read workspace skills from `.github/skills`, but the
            // user-global personal skills dir is `~/.copilot/skills` - the namespace differs by scope.
            ["copilot"] = new(global => global
                ? Path.Combine(UserProfile, ".copilot", "skills")
                : Path.Combine(Environment.CurrentDirectory, ".github", "skills")),

            // `github` is the workspace dir name a user may reach for; same targets as copilot.
            ["github"] = new(global => global
                ? Path.Combine(UserProfile, ".copilot", "skills")
                : Path.Combine(Environment.CurrentDirectory, ".github", "skills")),

            // Goose reads a project `.goose/skills`, but its user-global skills live under the
            // portable `~/.agents/skills` (not `~/.goose`).
            ["goose"] = new(global => global
                ? Path.Combine(UserProfile, ".agents", "skills")
                : Path.Combine(Environment.CurrentDirectory, ".goose", "skills")),
        };

    public static void Install(string? agent, bool global)
    {
        agent ??= "agents";

        var target = KnownAgents.TryGetValue(agent, out var known) ? known : Convention(agent);
        var destinationPath = target.ResolveSkillsDirectory(global);

        Log.Information("Installing skills to {SkillsPath}", destinationPath);

        var sourcePath = Path.Combine(AppContext.BaseDirectory, "Skills");

        foreach (var skillSourceDirectory in Directory.EnumerateDirectories(sourcePath))
        {
            var skillName = Path.GetFileName(skillSourceDirectory);
            var destination = Path.Combine(destinationPath, skillName);

            Log.Information("Installing skill {SkillName} to destination path {SkillPath}", skillName, destinationPath);

            CopyFilesRecursive(skillSourceDirectory, destination);
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