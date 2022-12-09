// Copyright 2020 Datalust Pty Ltd and Contributors
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

namespace SeqCli.Apps.Definitions;

// Matches https://github.com/datalust/seq-apps-runtime/blob/dev/src/Seq.Apps/Apps/SettingInputType.cs
public enum AppSettingType
{
    Text,
    LongText,
    Checkbox,
    Integer,
    Decimal,
    Password,
        
    // Not mirrored in Seq.Apps; currently, only available to C# apps when the input type is left as
    // Unspecified, and the corresponding property is an enum type.
    Select,

    // Unused; required for (very early) legacy app support.
    Number = 1000
}