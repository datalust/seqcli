namespace SeqCli.Config.Forwarder;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

public class SeqCliForwarderStorageConfig
{
    public long TargetChunkSizeBytes { get; set; } = 10 * 512 * 1024;
    public int? MaxChunks { get; set; } = null;
}
