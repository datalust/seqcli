using Seq.Forwarder.Multiplexing;
using Seq.Forwarder.Shipper;
using Seq.Forwarder.Tests.Support;
using SeqCli.Tests.Support;
using Xunit;

namespace Seq.Forwarder.Tests.Shipper
{
    public class ServerResponseProxyTests
    {
        [Fact]
        public void WhenNoResponseRecordedEmptyIsReturned()
        {
            var proxy = new ServerResponseProxy();
            var response = proxy.GetResponseText(Some.ApiKey());
            Assert.Equal("{}", response);
        }

        [Fact]
        public void WhenApiKeysDontMatchEmptyResponseReturned()
        {
            var proxy = new ServerResponseProxy();
            proxy.SuccessResponseReturned(Some.ApiKey(), "this is never used");
            var response = proxy.GetResponseText(Some.ApiKey());
            Assert.Equal("{}", response);
        }

        [Fact]
        public void WhenApiKeysMatchTheResponseIsReturned()
        {
            var proxy = new ServerResponseProxy();
            var apiKey = Some.ApiKey();
            var responseText = "some response";
            proxy.SuccessResponseReturned(apiKey, responseText);
            var response = proxy.GetResponseText(apiKey);
            Assert.Equal(responseText, response);
        }

        [Fact]
        public void NullApiKeysAreConsideredMatching()
        {
            var proxy = new ServerResponseProxy();
            var responseText = "some response";
            proxy.SuccessResponseReturned(null, responseText);
            var response = proxy.GetResponseText(null);
            Assert.Equal(responseText, response);
        }
    }
}
