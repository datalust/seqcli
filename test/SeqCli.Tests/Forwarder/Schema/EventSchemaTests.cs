using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeqCli.Forwarder.Schema;
using Xunit;

namespace SeqCli.Tests.Forwarder.Schema
{
    public class EventSchemaTests
    {
        static readonly JsonSerializer RawSerializer = JsonSerializer.Create(
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });

        [Fact]
        public void ClefNormalizationAcceptsDuplicateRenderings()
        {
            var payload = "{\"@t\": \"2015-05-09T12:09:08.12345Z\"," +
                          " \"@mt\": \"{A:000} and {A:000}\"," +
                          " \"@r\": [\"424\",\"424\"]}";

            AssertCanNormalizeClef(payload);
        }

        [Fact]
        public void ClefNormalizationPropagatesRenderings()
        {
            const string payload = "{\"@t\":\"2018-12-02T09:05:47.256725+03:00\",\"@mt\":\"Hello {P:000}!\",\"P\":12,\"@r\":[\"012\"]}";
            var evt = AssertCanNormalizeClef(payload);
            Assert.Single(evt.Renderings);
        }

        [Fact]
        public void ClefNormalizationIgnoresMissingRenderings()
        {
            const string payload = "{\"@t\":\"2018-12-02T09:05:47.256725+03:00\",\"@mt\":\"Hello {P:000}!\",\"P\":12}";
            AssertCanNormalizeClef(payload);
        }

        [Fact]
        public void ClefNormalizationFixesTooFewRenderings1()
        {
            const string payload = "{\"@t\":\"2018-12-02T09:05:47.256725+03:00\",\"@mt\":\"Hello {P:000}!\",\"P\":12,\"@r\":[]}";
            var evt = AssertCanNormalizeClef(payload);
            Assert.Null(evt.Renderings);
        }

        [Fact]
        public void ClefNormalizationFixesTooFewRenderings2()
        {
            const string payload = "{\"@t\":\"2018-12-02T09:05:47.256725+03:00\",\"@mt\":\"Hello {P:000} {Q:x}!\",\"P\":12,\"@r\":[\"012\"]}";
            var evt = AssertCanNormalizeClef(payload);
            Assert.Null(evt.Renderings);
        }

        [Fact]
        public void ClefNormalizationIgnoresTooManyRenderings()
        {
            const string payload = "{\"@t\":\"2018-12-02T09:05:47.256725+03:00\",\"@mt\":\"Hello {P:000}!\",\"P\":12,\"@r\":[\"012\",\"013\"]}";
            var evt = AssertCanNormalizeClef(payload);
            Assert.Null(evt.Renderings);
        }

        static dynamic AssertCanNormalizeClef(string payload)
        {
            var jo = RawSerializer.Deserialize<JObject>(new JsonTextReader(new StringReader(payload)))!;

            var valid = EventSchema.FromClefFormat(1, jo, out var rawFormat, out var error);
            Assert.True(valid, error);
            Assert.NotNull(rawFormat);
            return rawFormat!;
        }
    }
}