using Seq.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;

namespace SeqCli.Connection
{
    class SeqConnectionFactory
    {
        public SeqConnection Connect(ConnectionFeature connection, SeqCliConfig config)
        {
            string url, apiKey;
            if (connection.IsUrlSpecified)
            {
                url = connection.Url;
                apiKey = connection.ApiKey;
            }
            else
            {
                url = config.Connection.ServerUrl;
                apiKey = config.Connection.ApiKey;
            }
            return new SeqConnection(url, apiKey);
        }
    }
}
