using System;
using System.Collections.Generic;
using System.Globalization;
using SeqCli.PlainText;
using SeqCli.PlainText.Extraction;
using SeqCli.PlainText.Patterns;
using Xunit;

namespace SeqCli.Tests.PlainText;

public class ExtractionPatternInterpreterTests
{
    static (IDictionary<string, object>, string) ExtractValues(string pattern, string candidate)
    {
        var parsed = ExtractionPatternParser.Parse(pattern);
        var extractor = ExtractionPatternInterpreter.CreateNameValueExtractor(parsed);
        return extractor.ExtractValues(candidate);
    }

    [Fact]
    public void NonGreedyMatchCanLookaheadMultipleTokens()
    {
        var (properties, remainder) = ExtractValues("[{test:**}]!", "[0]abc[1]!");
        Assert.Null(remainder);
        Assert.Equal("0]abc[1", properties["test"].ToString());
    }
        
    [Fact]
    public void TheMatchingPatternCanExtractDefaultSerilogFileOutput()
    {
        // This is the default format: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        // See: https://github.com/serilog/serilog-sinks-file#controlling-event-formatting
            
        // {@l:ident} is required so that the default "token" pattern doesn't greedily eat up the `]`.
        // "timestamp" is intended to be an aggregate timestamp parser that tries ISO 8601, RFC 2822, and various other
        // popular timestamp formats.

        var pattern = "{@t:timestamp} [{@l:ident}] {@m:*}{:n}{@x:*}";

        var candidate =
            @"2018-02-21 13:29:00.123 +10:00 [ERR] The operation failed
System.DivideByZeroException: Attempt to divide by zero
  at SomeClass.SomeMethod()
";

        var (properties, remainder) = ExtractValues(pattern, candidate);
            
        Assert.Equal(
            DateTimeOffset.ParseExact("2018-02-21 13:29:00.123 +10:00", "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
            properties["@t"]);
        Assert.Equal("ERR", properties["@l"].ToString());
        Assert.Equal("The operation failed", properties["@m"].ToString());
        Assert.Equal(@"System.DivideByZeroException: Attempt to divide by zero
  at SomeClass.SomeMethod()
", properties["@x"].ToString());
        Assert.Null(remainder);
    }
        
    [Fact(Skip = "Work in progress")]
    public void TheMatchingPatternCanExtractDefaultSerilogConsoleOutput()
    {
        // This is the default format: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        // See: https://github.com/serilog/serilog-sinks-console#output-templates

        // "localtime" will add the closest non-future date to the time component that is matched
        // by the pattern
            
        // The pattern language needs to be extended here so that the brackets, timestamp, spacing and
        // level are all used as the start-frame marker. The strawman syntax proposes that to the
        // right of `:` will always be either an alphanumeric matcher name, or a subexpression. This
        // does have the issue that `{?:foo}` would be ambiguous (optional 'foo' matcher or optional 'foo'
        // literal, so some escaping would be necessary - e.g. `{?:\foo}` to indicate a literal 'foo' and
        // `{?:\*}` for an optional literal asterisk, `{?:\\}` for an optional literal backslash.
            
#pragma warning disable 219
        var pattern = "{:[{@t:localtime} {@l:ident}] }{@m:*}{:n}{@x:*}";
#pragma warning restore 219
    }
        
    [Fact(Skip = "Work in progress")]
    public void OptionalSourceContextCanBeExtracted()
    {
        // The {?: optional grouping is just an anonymous optional property, e.g. if the formatting was
        // not dynamic, it might be written {SourceContext?:*}; using the grouping means the surrounding
        // whitespace and parens are required only if the optional group is matched.
#pragma warning disable 219
        var pattern = "{:[{@t} {@l:ident}] }{?:({SourceContext:*}) }{@m:*}{:n}{@x:*}";
#pragma warning restore 219
    }
}