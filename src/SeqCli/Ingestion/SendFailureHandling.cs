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

namespace SeqCli.Ingestion
{
    /// <summary>
    /// Controls how connection failures during ingestion are handled.
    /// </summary>
    /// <remarks>A 'retry' option will appear here at some future point.</remarks>
    enum SendFailureHandling
    {
        /// <summary>
        /// Log error information and exit.
        /// </summary>
        Fail,
        
        /// <summary>
        /// Log error information, drop the failed batch, and continue.
        /// </summary>
        Continue,
        
        /// <summary>
        /// Silently ignore failures.
        /// </summary>
        Ignore
    }
}
