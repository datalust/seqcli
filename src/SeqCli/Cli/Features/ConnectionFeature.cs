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

using SeqCli.Util;

namespace SeqCli.Cli.Features
{
    class ConnectionFeature : CommandFeature
    {
        public ConnectionFeature()
        {
            Url = "";
            ApiKey = "";
        }

        public bool IsUrlSpecified => !string.IsNullOrEmpty(Url);
        public bool IsApiKeySpecified => !string.IsNullOrEmpty(ApiKey);

        public string Url { get; set; }
        public string ApiKey { get; set; }

        public override void Enable(OptionSet options)
        {
            options.Add("s=|server=",
                "The URL of the Seq server; by default the `connection.serverUrl` value will be used",
                v => Url = ArgumentString.Normalize(v));

            options.Add("a=|apikey=",
                "The API key to use when connecting to the server; by default `connection.apiKey` value will be used",
                v => ApiKey = ArgumentString.Normalize(v));
        }
    }
}
