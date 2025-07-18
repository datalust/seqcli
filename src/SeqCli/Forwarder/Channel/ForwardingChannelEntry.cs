using System;
using System.Threading.Tasks;

namespace SeqCli.Forwarder.Channel;

public readonly record struct ForwardingChannelEntry(ArraySegment<byte> Data, TaskCompletionSource CompletionSource);
