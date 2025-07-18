﻿// Copyright 2018 Datalust Pty Ltd
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
using Seq.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Encryptor;

namespace SeqCli.Connection;

class SeqConnectionFactory
{
    public SeqConnection Connect(ConnectionFeature connection, SeqCliConfig config)
    {
        var (url, apiKey) = GetConnectionDetails(connection, config);
        return new SeqConnection(url, apiKey);
    }
        
    public (string? serverUrl, string? apiKey) GetConnectionDetails(ConnectionFeature connection, SeqCliConfig config)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));

        string? url, apiKey;
        if (connection.IsUrlSpecified)
        {
            url = connection.Url;
            apiKey = connection.ApiKey;
        }
        else if (connection.IsProfileNameSpecified)
        {
            if (!config.Profiles.TryGetValue(connection.ProfileName!, out var profile))
                throw new ArgumentException($"A profile named `{connection.ProfileName}` was not found; see `seqcli profile list` for available profiles.");
                
            url = profile.ServerUrl;
            apiKey = profile.DecodeApiKey(config.Encryption.DataProtector());
        }
        else
        {
            url = config.Connection.ServerUrl;
            apiKey = connection.IsApiKeySpecified ? connection.ApiKey : config.Connection.DecodeApiKey(config.Encryption.DataProtector());
        }

        return (url, apiKey);
    }
}