// Copyright 2018 Datalust Pty Ltd and Contributors
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

namespace SeqCli.Util
{
    static class Presentation
    {
        /// <summary>
        /// Formats <paramref name="ex"/> as its message, with any inner exception messages
        /// listed following it in (parens).
        /// </summary>
        /// <param name="ex">The exception to format.</param>
        /// <returns>A friendlier, but reasonably complete, rendering of the exception's message
        /// and causal chain.</returns>
        public static string FormattedMessage(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            var baseException = ex.GetBaseException();
            var message = new StringBuilder(baseException.Message);

            var inner = baseException.InnerException?.GetBaseException();
            if (inner != null)
            {
                message.Append(" (");
                while (inner != null)
                {
                    message.Append(inner.Message);
                    inner = inner.InnerException.GetBaseException();
                    if (inner != null)
                        message.Append(" ");
                }
                message.Append(")");
            }

            return message.ToString();
        }
    }
}
