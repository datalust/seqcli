// Copyright © Datalust Pty Ltd and Contributors
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

using System.Collections.Generic;

namespace SeqCli.Forwarder.Multiplexing;

public class ServerResponseProxy
{
    const string EmptyResponse = "{}";

    readonly object _syncRoot = new();
    readonly Dictionary<string, string> _lastResponseByApiKey = new();
    string _lastNoApiKeyResponse = EmptyResponse;

    public void SuccessResponseReturned(string? apiKey, string response)
    {
        lock (_syncRoot)
        {
            if (apiKey == null)
                _lastNoApiKeyResponse = response;
            else
                _lastResponseByApiKey[apiKey] = response;
        }
    }

    public string GetResponseText(string? apiKey)
    {
        lock (_syncRoot)
        {
            if (apiKey == null)
                return _lastNoApiKeyResponse;

            if (_lastResponseByApiKey.TryGetValue(apiKey, out var response))
                return response;

            return EmptyResponse;
        }
    }
}