using System;
using System.Threading.Tasks;

namespace SeqCli.Forwarder.Channel;

public readonly record struct LogChannelEntry(byte[] Storage, Range Range, TaskCompletionSource Completion);
