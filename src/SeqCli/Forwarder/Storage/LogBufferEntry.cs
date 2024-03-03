using System;
using System.Threading.Tasks;

namespace SeqCli.Forwarder.Storage;

public readonly record struct LogBufferEntry(byte[] Storage, Range Range, TaskCompletionSource Completion);
