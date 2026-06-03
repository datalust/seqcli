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

namespace SeqCli.Output;

enum OutputSyntax
{
    /// <summary>
    /// Plain, human-readable text.
    /// </summary>
    Text,
    /// <summary>
    /// JSON (newline-delimited for JSON streams).
    /// </summary>
    Json,
    /// <summary>
    /// Seq's native value syntax. This is intended for agent use: values presented
    /// in Seq's native syntax are more reliably fed back into searches/queries.
    /// </summary>
    Native
}