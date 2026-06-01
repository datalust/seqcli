#nullable enable
using System;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using SeqCli.Mcp;
using SeqCli.Tests.Support;
using Xunit;

namespace SeqCli.Tests.Mcp;

public class McpSessionTests
{
    [Fact]
    public void ResultIdsRoundTrip()
    {
        const int id = 1245;
        var formatted = McpSession.FormatResultId(id);
        Assert.True(McpSession.TryParseResultId(formatted, out var rt));
        Assert.Equal(id, rt);
    }

    [Fact]
    public void ImportingTheSameEventReturnsTheSameId()
    {
        var session = new McpSession();

        var first = session.ImportSearchResult(Some.MakeEvent(e => e.Id = "event-1"));
        var second = session.ImportSearchResult(Some.MakeEvent(e => e.Id = "event-1"));

        Assert.Equal(first, second);
    }

    [Fact]
    public void ImportingDistinctEventsReturnsDistinctIds()
    {
        var session = new McpSession();

        var first = session.ImportSearchResult(Some.MakeEvent(e => e.Id = "event-1"));
        var second = session.ImportSearchResult(Some.MakeEvent(e => e.Id = "event-2"));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ImportedEventsCanBeRetrievedById()
    {
        var session = new McpSession();
        var evt = Some.MakeEvent();

        var resultId = session.ImportSearchResult(evt);

        Assert.True(session.TryGetSearchResult(resultId, out var result, out var error));
        Assert.Same(evt, result);
        Assert.Null(error);
    }

    [Fact]
    public void MalformedResultIdsAreRejected()
    {
        var session = new McpSession();

        Assert.False(session.TryGetSearchResult("not-a-result-id", out var result, out var error));
        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public void WellFormedButUnknownResultIdsReturnAnError()
    {
        var session = new McpSession();
        var unknown = McpSession.FormatResultId(999);

        Assert.False(session.TryGetSearchResult(unknown, out var result, out var error));
        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public void NoUserPropertyNamesAreEnumeratedWithoutResults()
    {
        var session = new McpSession();

        Assert.Empty(session.EnumerateUserPropertyNames(CancellationToken.None));
    }

    [Fact]
    public void UserPropertyNamesAreEnumeratedAcrossPropertiesScopeAndResource()
    {
        var session = new McpSession();
        session.ImportSearchResult(Some.MakeEvent(e =>
        {
            e.Id = "event-1";
            e.Properties = Some.MakeProperties(("UserId", 42));
            e.Scope = Some.MakeProperties(("name", "my-scope"));
            e.Resource = Some.MakeProperties(("service", new JObject { ["name"] = "web" }));
        }));

        var names = session.EnumerateUserPropertyNames(CancellationToken.None).ToList();

        Assert.Contains("UserId", names);
        Assert.Contains("@Scope.name", names);
        Assert.Contains("@Resource.service", names);
        Assert.Contains("@Resource.service.name", names);
    }

    [Fact]
    public void UserPropertyNamesAreDeduplicatedAcrossResults()
    {
        var session = new McpSession();
        session.ImportSearchResult(Some.MakeEvent(e =>
        {
            e.Id = "event-1";
            e.Properties = Some.MakeProperties(("UserId", 1));
        }));
        session.ImportSearchResult(Some.MakeEvent(e =>
        {
            e.Id = "event-2";
            e.Properties = Some.MakeProperties(("UserId", 2));
        }));

        var names = session.EnumerateUserPropertyNames(CancellationToken.None).ToList();

        Assert.Equal(["UserId"], names);
    }

    [Fact]
    public void EnumeratingUserPropertyNamesObservesCancellation()
    {
        var session = new McpSession();
        session.ImportSearchResult(Some.MakeEvent(e => e.Properties = Some.MakeProperties(("UserId", 42))));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => session.EnumerateUserPropertyNames(cts.Token).ToList());
    }

    [Fact]
    public void ClearForgetsImportedResults()
    {
        var session = new McpSession();
        var resultId = session.ImportSearchResult(Some.MakeEvent());

        session.Clear();

        Assert.False(session.TryGetSearchResult(resultId, out var result, out var error));
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Empty(session.EnumerateUserPropertyNames(CancellationToken.None));
    }

    [Fact]
    public void ClearPreservesTheResultIdSequence()
    {
        var session = new McpSession();
        var first = session.ImportSearchResult(Some.MakeEvent(e => e.Id = "event-1"));

        session.Clear();

        var second = session.ImportSearchResult(Some.MakeEvent(e => e.Id = "event-1"));
        Assert.NotEqual(first, second);
    }
}
