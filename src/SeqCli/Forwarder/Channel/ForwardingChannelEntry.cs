using System;
using System.Threading.Tasks;

namespace SeqCli.Forwarder.Channel;

readonly record struct ForwardingChannelEntry(ArraySegment<byte> Data, TaskCompletionSource CompletionSource);
