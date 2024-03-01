// Copyright © Datalust Pty Ltd
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
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using Serilog;

namespace SeqCli.Cli.Commands.Forwarder;

[Command("forwarder", "truncate", "Empty the forwarder's persistent log buffer")]
class TruncateCommand : Command
{
    readonly StoragePathFeature _storagePath;
    readonly ConfirmFeature _confirm;

    public TruncateCommand()
    {
        _storagePath = Enable<StoragePathFeature>();
        _confirm = Enable<ConfirmFeature>();
    }

    protected override async Task<int> Run(string[] args)
    {
        try
        {
            if (!_confirm.TryConfirm("All data in the forwarder's log buffer will be deleted. This cannot be undone."))
                return 1;
                
            return 0;
        }
        catch (Exception ex)
        {
            await using var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            logger.Fatal(ex, "Could not truncate log buffer");
            return 1;
        }
    }
}