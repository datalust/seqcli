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

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Superpower;
using Superpower.Model;

namespace SeqCli.PlainText.Framing
{
    class FrameReader
    {
        readonly TextReader _source;
        readonly TimeSpan _trailingLineArrivalDeadline;
        readonly TextParser<TextSpan> _frameStart;

        string _unconsumedFirstLine;
        Task<string> _unawaitedNextLine;

        public FrameReader(TextReader source, TextParser<TextSpan> frameStart, TimeSpan trailingLineArrivalDeadline)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _frameStart = frameStart ?? throw new ArgumentNullException(nameof(frameStart));
            _trailingLineArrivalDeadline = trailingLineArrivalDeadline;
        }

        public async Task<Frame> TryReadAsync()
        {
            var valueBuilder = new StringBuilder();
            var hasValue = false;

            if (_unconsumedFirstLine != null)
            {
                valueBuilder.AppendLine(_unconsumedFirstLine);
                _unconsumedFirstLine = null;
                hasValue = true;
            }
            else if (_unawaitedNextLine != null)
            {
                var index = Task.WaitAny(new Task[] {_unawaitedNextLine}, _trailingLineArrivalDeadline);
                if (index == -1)
                    return new Frame();
                
                var line = await _unawaitedNextLine;
                _unawaitedNextLine = null;
                if (line == null)
                    return new Frame {IsAtEnd = true};

                valueBuilder.AppendLine(line);
                hasValue = true;

                if (!IsFrameStart(line))
                    return new Frame {HasValue = true, IsOrphan = true, Value = valueBuilder.ToString()};
            }

            Task<string> readLine = null;
            while (true)
            {
                readLine = readLine ?? Task.Run(_source.ReadLineAsync);                
                var index = Task.WaitAny(new Task[] {readLine}, _trailingLineArrivalDeadline);
                if (index == -1)
                {
                    if (hasValue)
                    {
                        _unawaitedNextLine = readLine;
                        return new Frame {HasValue = true, Value = valueBuilder.ToString()};
                    }

                    // else, around we go!
                }
                else
                {
                    var line = await readLine;
                    readLine = null;
                    if (line == null)
                    {
                        if (hasValue)
                        {
                            return new Frame {HasValue = true, Value = valueBuilder.ToString(), IsAtEnd = true};
                        }

                        return new Frame();
                    }

                    if (IsFrameStart(line))
                    {
                        if (hasValue)
                        {
                            _unconsumedFirstLine = line;
                            return new Frame {HasValue = true, Value = valueBuilder.ToString()};
                        }

                        valueBuilder.AppendLine(line);
                        hasValue = true;
                    }
                    else
                    {
                        if (!hasValue)
                        {
                            valueBuilder.AppendLine(line);
                            return new Frame {HasValue = true, Value = valueBuilder.ToString(), IsOrphan = true};
                        }

                        valueBuilder.AppendLine(line);
                    }
                }
            }

            bool IsFrameStart(string line)
            {
                if (line == null) throw new ArgumentNullException(nameof(line));
                var result = _frameStart(new TextSpan(line));
                return result.HasValue && result.Value.Length > 0;
            }
        }
    }
}
