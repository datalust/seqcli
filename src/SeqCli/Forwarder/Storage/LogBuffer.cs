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
using System.Collections.Generic;
using Serilog;

namespace SeqCli.Forwarder.Storage
{
    public class LogBuffer : IDisposable
    {
        readonly ulong _bufferSizeBytes;
        // readonly LightningEnvironment _env;
        readonly object _sync = new object();
        bool _isDisposed;
        ulong _nextId = 0, _entries = 0, _writtenSinceRotateCheck;

        public LogBuffer(string bufferPath, ulong bufferSizeBytes)
        {
            _bufferSizeBytes = bufferSizeBytes;
            if (bufferPath == null) throw new ArgumentNullException(nameof(bufferPath));

            // _env = new LightningEnvironment(bufferPath)
            // {
            //     // Sparse; we'd hope fragmentation never gets this bad...
            //     MapSize = (long) bufferSizeBytes*10
            // };
            //
            // _env.Open();
            //
            // using (var tx = _env.BeginTransaction())
            // using (var db = tx.OpenDatabase())
            // {
            //     using (var cur = tx.CreateCursor(db))
            //     {
            //         if (!cur.MoveToLast())
            //         {
            //             _nextId = 1;
            //         }
            //         else
            //         {
            //             var current = cur.GetCurrent();
            //             _nextId = ByteKeyToULongKey(current.Key) + 1;
            //             _entries = (ulong) tx.GetEntriesCount(db);
            //         }
            //     }
            // }
            
            Log.Information("Log buffer open on {BufferPath}; {Entries} entries, next key will be {NextId}", bufferPath, _entries, _nextId);
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    // _env.Dispose();                    
                }
            }
        }

        public void Enqueue(byte[][] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            lock (_sync)
            {
                RequireNotDisposed();

                // var totalPayloadWritten = 0UL;
                //
                // using (var tx = _env.BeginTransaction())
                // using (var db = tx.OpenDatabase())
                // {
                //     foreach (var v in values)
                //     {
                //         if (v == null) throw new ArgumentException("Value array may not contain null.");
                //
                //         tx.Put(db, ULongKeyToByteKey(_nextId++), v);
                //         totalPayloadWritten += (ulong) v.Length;
                //     }
                //
                //     tx.Commit();
                //     _entries += (ulong) values.Length;
                //     _writtenSinceRotateCheck += totalPayloadWritten;
                // }

                RotateIfRequired();
            }
        }

        void RotateIfRequired()
        {
            if (_writtenSinceRotateCheck < _bufferSizeBytes/10)
                return;

            _writtenSinceRotateCheck = 0;
            //
            // using (var tx = _env.BeginTransaction())
            // using (var db = tx.OpenDatabase())
            // {
            //     int err;
            //     if (0 != (err = Lmdb.mdb_env_info(_env.Handle(), out var estat)))
            //         throw new Exception(Lmdb.mdb_strerror(err));
            //
            //     MDBStat stat;
            //     if (0 != (err = Lmdb.mdb_stat(tx.Handle(), db.Handle(), out stat)))
            //         throw new Exception(Lmdb.mdb_strerror(err));
            //
            //     // http://www.openldap.org/lists/openldap-technical/201303/msg00145.html
            //     // 1) MDB_stat gives you the page size.
            //     // 2) MDB_envinfo tells the mapsize and the last_pgno.If you divide mapsize 
            //     //    by pagesize you'll get max pgno. The MAP_FULL error is returned when last_pgno reaches max pgno.
            //
            //     var targetPages = _bufferSizeBytes/stat.ms_psize;
            //     if ((ulong) estat.me_last_pgno < targetPages && (double) (ulong) estat.me_last_pgno/targetPages < 0.75)
            //         return;
            //
            //     var count = tx.GetEntriesCount(db);
            //     if (count == 0)
            //     {
            //         Log.Warning("Attempting to rotate buffer but no events are present");
            //         return;
            //     }
            //
            //     var toPurge = Math.Max(count / 4, 1);
            //     Log.Warning("Buffer is full; dropping {ToPurge} events to make room for new ones",
            //         toPurge);
            //
            //     using (var cur = tx.CreateCursor(db))
            //     {
            //         cur.MoveToFirst();
            //
            //         for (var i = 0; i < toPurge; ++i)
            //         {
            //             cur.Delete();
            //             cur.MoveNext();
            //         }
            //     }
            //
            //     tx.Commit();
            // }
        }

        public LogBufferEntry[] Peek(int maxValueBytesHint)
        {
            lock (_sync)
            {
                RequireNotDisposed();

                var entries = new List<LogBufferEntry>();
                //
                // using (var tx = _env.BeginTransaction(TransactionBeginFlags.ReadOnly))
                // using (var db = tx.OpenDatabase())
                // {
                //     using (var cur = tx.CreateCursor(db))
                //     {
                //         if (cur.MoveToFirst())
                //         {
                //             var entriesBytes = 0;
                //
                //             do
                //             {
                //                 var current = cur.GetCurrent();
                //                 var entry = new LogBufferEntry
                //                 {
                //                     Key = ByteKeyToULongKey(current.Key),
                //                     Value = current.Value
                //                 };
                //
                //                 entriesBytes += entry.Value.Length;
                //                 if (entries.Count != 0 && entriesBytes > maxValueBytesHint)
                //                     break;
                //
                //                 entries.Add(entry);
                //
                //             } while (cur.MoveNext());
                //         }
                //     }
                // }

                return entries.ToArray();
            }
        }

        public void Dequeue(ulong toKey)
        {
            lock (_sync)
            {
                RequireNotDisposed();

                // ulong deleted = 0;
                //
                // using (var tx = _env.BeginTransaction())
                // using (var db = tx.OpenDatabase())
                // {
                //     using (var cur = tx.CreateCursor(db))
                //     {
                //         if (cur.MoveToFirst())
                //         {
                //             do
                //             {
                //                 var current = cur.GetCurrent();
                //                 if (ByteKeyToULongKey(current.Key) > toKey)
                //                     break;
                //
                //                 cur.Delete();
                //                 deleted++;
                //             } while (cur.MoveNext());
                //         }
                //     }
                //
                //     tx.Commit();
                //     _entries -= deleted;
                // }
            }
        }

        void RequireNotDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(typeof(LogBuffer).FullName);
        }

        static ulong ByteKeyToULongKey(byte[] key)
        {
            var copy = new byte[key.Length];
            for (var i = 0; i < key.Length; ++i)
                copy[copy.Length - (i + 1)] = key[i];

            return BitConverter.ToUInt64(copy, 0);
        }

        static byte[] ULongKeyToByteKey(ulong key)
        {
            var k = BitConverter.GetBytes(key);
            Array.Reverse(k);
            return k;
        }

        public void Enumerate(Action<ulong, byte[]> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            lock (_sync)
            {
                RequireNotDisposed();

                // using (var tx = _env.BeginTransaction(TransactionBeginFlags.ReadOnly))
                // using (var db = tx.OpenDatabase())
                // {
                //     using (var cur = tx.CreateCursor(db))
                //     {
                //         if (cur.MoveToFirst())
                //         {
                //             do
                //             {
                //                 var current = cur.GetCurrent();
                //                 action(ByteKeyToULongKey(current.Key), current.Value);
                //             } while (cur.MoveNext());
                //         }
                //     }
                // }
            }
        }
    }
}
