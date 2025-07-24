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

using SeqCli.Cli;

namespace SeqCli.Forwarder.Cli.Features
{
    class ServiceCredentialsFeature : CommandFeature
    {
        public bool IsUsernameSpecified => !string.IsNullOrEmpty(Username);
        public bool IsPasswordSpecified => !string.IsNullOrEmpty(Password);

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public override void Enable(OptionSet options)
        {
            options.Add("u=|username=",
                "The name of a Windows account to run the service under; if not specified the `NT AUTHORITY\\LocalService` account will be used",
                v => Username = v.Trim());

            options.Add("p=|password=",
                "The password for the Windows account to run the service under",
                v => Password = v.Trim());
        }
    }
}
