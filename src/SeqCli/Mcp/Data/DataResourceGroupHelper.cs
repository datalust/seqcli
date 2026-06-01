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
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seq.Api;
using Seq.Api.Model.Data;

namespace SeqCli.Mcp.Data;

public static class DataResourceGroupHelper
{
    static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        DateParseHandling = DateParseHandling.None,
        Culture = CultureInfo.InvariantCulture,
        FloatParseHandling = FloatParseHandling.Decimal,
    });

    public static async Task<QueryResultPart> QueryPreserveErrorResponsesAsync(SeqConnection connection, string query, CancellationToken cancellationToken = default)
    {
        // Unfortunately, the `Data.QueryAsync()` API throws when the server 400s, making this case tricky. Suggests
        // we should make some API client improvements...
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri("api/data?q=" + Uri.EscapeDataString(query), UriKind.Relative),
            Method = HttpMethod.Post, Content = new StringContent("{}", new UTF8Encoding(false), "application/json")
        };
        var response = await connection.Client.HttpClient.SendAsync(request, cancellationToken);
        return Serializer.Deserialize<QueryResultPart>(
            new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken))))!;
    }
}