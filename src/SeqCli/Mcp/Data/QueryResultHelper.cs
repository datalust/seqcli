// Copyright © Datalust and contributors.
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
using System.Collections.Generic;
using System.Linq;
using Seq.Api.Model.Data;

namespace SeqCli.Mcp.Data;

public static class QueryResultHelper
{
    /// <summary>
    /// Convert <paramref name="result"/> into a flat table. Seq reduces browser-side processing and optimizes
    /// response sizes by constructing result trees for some grouped/time-sliced query results.
    /// </summary>
    public static void Flatten(QueryResultPart result, Action<IEnumerable<object?>> writeRow)
    {
        if (result.Error != null)
            return;
                
        if (result.Rows != null)
        {
            writeRow(result.Columns!);
            foreach (var row in result.Rows)
            {
                writeRow(row);
            }
        }
        else if (result.Slices != null)
        {
            writeRow(new object[] {"time"}.Concat(result.Columns!));

            var empty = result.Columns!.Select(_ => "").ToArray();
            foreach (var slice in result.Slices)
            {
                var any = false;
                foreach (var row in slice.Rows)
                {
                    any = true;
                    writeRow(new object[] { DateTimeOffset.Parse(slice.Time).UtcDateTime }.Concat(row));
                }
                if (!any)
                {
                    writeRow(new object[] { DateTimeOffset.Parse(slice.Time).UtcDateTime }.Concat(empty));
                }
            }
        }
        else if (result.Series != null)
        {
            writeRow(MergeColumns(result.Columns!, result.Series.FirstOrDefault()));
            foreach (var series in result.Series)
            {
                foreach (var slice in series.Slices)
                {
                    var empty = result.Columns!.Take(series.Key.Length).Select(_ => (object?)null).ToArray();
                    var any = false;
                    foreach (var row in slice.Rows)
                    {
                        any = true;
                        writeRow(series.Key.Concat([DateTimeOffset.Parse(slice.Time).UtcDateTime]).Concat(row));
                    }
                    if (!any)
                    {
                        writeRow(series.Key.Concat([DateTimeOffset.Parse(slice.Time).UtcDateTime]).Concat(empty));
                    }
                }
            }
        }
        else
        {
            throw new NotImplementedException("Query result set does not conform to any expected pattern.");
        }            
    }
    
    static IEnumerable<object> MergeColumns(string[] columns, TimeseriesPart? firstSeries)
    {
        if (firstSeries == null)
            yield break;

        var i = 0;
        for (; i < firstSeries.Key.Length; ++i)
        {
            yield return columns[i];
        }

        yield return "time";

        for (; i < columns.Length; ++i)
        {
            yield return columns[i];
        }
    }
}