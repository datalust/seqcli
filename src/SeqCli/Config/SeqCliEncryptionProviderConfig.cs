// Copyright 2024 Datalust Pty Ltd
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
using System.Runtime.InteropServices;
using SeqCli.Encryptor;

namespace SeqCli.Config;

class SeqCliEncryptionProviderConfig
{
    public string? Encryptor { get; set; }
    public string? EncryptorArgs { get; set; }
    
    public string? Decryptor { get; set; }
    public string? DecryptorArgs { get; set; }

    public IDataProtector DataProtector()
    {
        if (!string.IsNullOrWhiteSpace(Encryptor) || !string.IsNullOrWhiteSpace(Decryptor))
        {
            if (string.IsNullOrWhiteSpace(Encryptor) || string.IsNullOrWhiteSpace(Decryptor))
            {
                throw new ArgumentException(
                    "If either of `encryption.encryptor` or `encryption.decryptor` is specified, both must be specified.");
            }
            
            return new ExternalDataProtector(Encryptor, EncryptorArgs, Decryptor, DecryptorArgs);
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new WindowsNativeDataProtector() : new PlaintextDataProtector();
    }
}