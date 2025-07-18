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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace SeqCli.Forwarder.Diagnostics;

class InMemorySink : ILogEventSink
{
    readonly int _queueLength;
    readonly ConcurrentQueue<LogEvent> _queue = new();
         
    public InMemorySink(int queueLength)
    {
        _queueLength = queueLength;
    }

    public IEnumerable<LogEvent> Read()
    {
        return _queue.ToArray();
    }

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        _queue.Enqueue(logEvent);

        while (_queue.Count > _queueLength)
        {
            _queue.TryDequeue(out _);
        }
    }
}