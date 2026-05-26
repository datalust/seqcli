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
using Serilog;

namespace SeqCli.Skills;

static class SkillInstaller
{
    public static void Install(string destinationPath)
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "Skills");
        
        foreach (var skillSourceDirectory in Directory.EnumerateDirectories(sourcePath))
        {
            var skillName = Path.GetFileName(skillSourceDirectory);
            var destination = Path.Combine(destinationPath, skillName);
            
            Log.Information("Installing skill {SkillName} to destination path {SkillPath}", skillName, destinationPath);
            
            CopyFilesRecursive(skillSourceDirectory, destination);
        }
    }

    static void CopyFilesRecursive(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        
        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            CopyFilesRecursive(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}