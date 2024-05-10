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
using System.Net.Http;

namespace SeqCli.Cli.Features;

class TimeoutFeature: CommandFeature
{
    int? _timeoutMS;

    public override void Enable(OptionSet options)
    {
        options.Add("timeout=", "The execution timeout in milliseconds", v => _timeoutMS = int.Parse(v?.Trim() ?? "0"));
    }

    // Ensures we don't forget to configure the client whenever the server is given a longer timeout than
    // the client's internal default.
    public TimeSpan? ApplyTimeout(HttpClient httpClient)
    {
        if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));

        var timeout = _timeoutMS.HasValue ? TimeSpan.FromMilliseconds(_timeoutMS.Value) : (TimeSpan?)null;
        if (timeout != null)
        {
            // The timeout is applied server-side; allowing an extra 10 seconds here means that the
            // user experience will be consistent - the error message will be the server's message, etc.
            httpClient.Timeout = timeout.Value.Add(TimeSpan.FromSeconds(10));
        }

        return timeout;
    }
}