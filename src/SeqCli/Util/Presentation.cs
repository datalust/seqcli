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
using System.Reflection;
using System.Text;

#nullable enable

namespace SeqCli.Util;

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

        static Exception Unwrap(Exception outer)
        {
            return outer is AggregateException or TargetInvocationException ? outer.GetBaseException() : outer;
        }

        static string Describe(Exception toDescribe)
        {
            // :-)
            return toDescribe.Message.Replace(", see inner exception", "");
        }

        var unwrapped = Unwrap(ex);
        var message = new StringBuilder(Describe(unwrapped));

        while (unwrapped.InnerException != null)
        {
            unwrapped = Unwrap(unwrapped.InnerException);
                    
            message.Append(' ');
            message.Append(Describe(unwrapped));
        }

        return message.ToString();
    }
}