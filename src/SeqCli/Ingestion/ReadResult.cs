using System.Text.Json.Nodes;

namespace SeqCli.Ingestion;

readonly struct ReadResult
{
    /// <summary>
    /// The event, as a JSON document in Seq's emission schema, or <c>null</c> if no event
    /// is available.
    /// </summary>
    public JsonObject? Document { get; }

    public bool IsAtEnd { get; }

    public ReadResult(JsonObject? document, bool isAtEnd)
    {
        Document = document;
        IsAtEnd = isAtEnd;
    }
}
