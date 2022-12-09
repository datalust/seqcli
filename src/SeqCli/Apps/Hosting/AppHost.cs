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
using System.Net;
using System.Threading.Tasks;
using Seq.Apps;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace SeqCli.Apps.Hosting;

static class AppHost
{
    public static async Task<int> Run(
        string packageBinaryPath,
        IReadOnlyDictionary<string, string> appSettings,
        string storagePath,
        string seqBaseUri,
        string appInstanceId,
        string appInstanceTitle,
        string? seqInstanceName = null,
        string? mainAppTypeName = null)
    {
        if (packageBinaryPath == null) throw new ArgumentNullException(nameof(packageBinaryPath));
        if (appSettings == null) throw new ArgumentNullException(nameof(appSettings));
        if (storagePath == null) throw new ArgumentNullException(nameof(storagePath));
        if (seqBaseUri == null) throw new ArgumentNullException(nameof(seqBaseUri));
        if (appInstanceId == null) throw new ArgumentNullException(nameof(appInstanceId));
        if (appInstanceTitle == null) throw new ArgumentNullException(nameof(appInstanceTitle));

        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

        await using var log = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .WriteTo.Console(new CompactJsonFormatter(), standardErrorFromLevel: LevelAlias.Minimum)
            .CreateLogger();
            
        try
        {
            var app = new App(appInstanceId, appInstanceTitle, appSettings, storagePath);
            var host = new Host(seqBaseUri, seqInstanceName);

            using var appContainer = new AppContainer(log, packageBinaryPath, app, host, mainAppTypeName);
            appContainer.StartPublishing(Console.Out);

            while (Console.ReadLine() is { } line)
            {
                await appContainer.SendAsync(line);
            }

            appContainer.StopPublishing();

            return 0;
        }
        catch (Exception ex)
        {
            log.Fatal(ex, "App host failed unexpectedly");
            return 1;
        }
    }
}