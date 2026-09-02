using System.Text.Json.Nodes;

namespace SeqCli.Ingestion;

struct BatchResult
{
    public JsonObject[] Documents { get; }
    public bool IsLast { get; }

    public BatchResult(JsonObject[] documents, bool isLast)
    {
        Documents = documents;
        IsLast = isLast;
    }
}
