﻿// Copyright 2018 Datalust Pty Ltd and Contributors
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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Seq.Api.Model.Signals;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;

namespace SeqCli.Cli.Commands.Signal;

[Command("signal", "import", "Import signals in newline-delimited JSON format",
    Example="seqcli signal import -i ./Exceptions.json")]
class ImportCommand : Command
{
    readonly FileInputFeature _fileInputFeature;
    readonly EntityOwnerFeature _entityOwner;
    readonly ConnectionFeature _connection;
    readonly StoragePathFeature _storagePath;
    
    bool _merge;

    readonly JsonSerializer _serializer = JsonSerializer.Create(
        new JsonSerializerSettings{
            Converters = { new StringEnumConverter() }
        });

    public ImportCommand()
    {
        Options.Add(
            "merge",
            "Update signals that have ids matching those in the imported data; the default is to always create new signals",
            _ => _merge = true);

        _fileInputFeature = Enable(new FileInputFeature("File to import"));
        _entityOwner = Enable(new EntityOwnerFeature("signal", "import", "imported"));
        _connection = Enable<ConnectionFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        using var input = _fileInputFeature.OpenSingleInput();
        var line = await input.ReadLineAsync();
        while (line != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                // Explicitly copying fields here ensures we don't try copying links or ids; for other
                // entity types it'll ensure we notice places that "referential integrity" has to be
                // maintained.
                var src = _serializer.Deserialize<SignalEntity>(new JsonTextReader(new StringReader(line)));
                if (src == null) continue;

                SignalEntity dest;
                if (_merge)
                {
                    try
                    {
                        dest = await connection.Signals.FindAsync(src.Id);
                    }
                    catch (Exception)
                    {
                        dest = await connection.Signals.TemplateAsync();
                    }
                }
                else
                {
                    dest = await connection.Signals.TemplateAsync();
                }
                    
                dest.Title = src.Title;
                dest.Description = src.Description;
                dest.ExplicitGroupName = src.ExplicitGroupName;
                dest.Grouping = src.Grouping;
                dest.IsProtected = src.IsProtected;
                dest.Filters = src.Filters;
                dest.Columns = src.Columns;
                dest.OwnerId = _entityOwner.OwnerId;

                if (_merge && dest.Id != null)
                {
                    await connection.Signals.UpdateAsync(dest);
                }
                else
                {
                    await connection.Signals.AddAsync(dest);
                }
            }

            line = await input.ReadLineAsync();
        }

        return 0;
    }
}