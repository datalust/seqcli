// Copyright 2018 Datalust Pty Ltd
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
using System.Text;
using Newtonsoft.Json;
using SeqCli.Encryptor;
using SeqCli.Util;

namespace SeqCli.Config;

public class ConnectionConfig
{
    const string ProtectedDataPrefix = "pd.";

    static readonly Encoding ProtectedDataEncoding = new UTF8Encoding(false);

    public string ServerUrl { get; set; } = "http://localhost:5341";

    [JsonProperty("apiKey")]
    public string? EncodedApiKey { get; set; }

    public string? DecodeApiKey(IDataProtector dataProtector)
    {
        if (string.IsNullOrWhiteSpace(EncodedApiKey))
            return null;
        
        if (!EncodedApiKey.StartsWith(ProtectedDataPrefix))
            return EncodedApiKey;

        return ProtectedDataEncoding.GetString(dataProtector.Decrypt(Convert.FromBase64String(EncodedApiKey[ProtectedDataPrefix.Length..])));
    }

    public void EncodeApiKey(string? apiKey, IDataProtector dataProtector)
    {
        if (apiKey == null)
        {
            EncodedApiKey = null;
            return;
        }

        var encoded = dataProtector.Encrypt(ProtectedDataEncoding.GetBytes(apiKey));

        EncodedApiKey = $"{ProtectedDataPrefix}{Convert.ToBase64String(encoded)}";
    }

    public uint? PooledConnectionLifetimeMilliseconds { get; set; } = null;
    public ulong EventBodyLimitBytes { get; set; } = 256 * 1024;
    public ulong PayloadLimitBytes { get; set; } = 10 * 1024 * 1024;
}