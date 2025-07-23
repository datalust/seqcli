// Copyright © Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Net.Http;
using System.Threading;
using Autofac;
using Seq.Api;
using SeqCli.Config;
using SeqCli.Forwarder.Channel;
using SeqCli.Forwarder.Web.Api;
using SeqCli.Forwarder.Web.Host;
using Serilog;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Templates;

namespace SeqCli.Forwarder;

class ForwarderModule : Module
{
    readonly string _bufferPath;
    readonly SeqCliConfig _config;
    readonly SeqConnection _connection;
    readonly string? _apiKey;

    public ForwarderModule(string bufferPath, SeqCliConfig config, SeqConnection connection, string? apiKey)
    {
        _bufferPath = bufferPath ?? throw new ArgumentNullException(nameof(bufferPath));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _connection = connection;
        _apiKey = apiKey;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ServerService>().SingleInstance();

        if (_config.Forwarder.UseApiKeyForwarding)
        {
            builder.Register<ApiKeyForwardingChannelWrapper>(_ =>  
                new ApiKeyForwardingChannelWrapper(_bufferPath, _connection, _config))
                .As<ForwardingChannelWrapper>().SingleInstance();
        }
        else
        {
            builder.Register<SeqCliConnectionForwardingChannelWrapper>(_ =>  
                new SeqCliConnectionForwardingChannelWrapper(_bufferPath, _connection, _config, _apiKey))
                .As<ForwardingChannelWrapper>().SingleInstance();
        }

        builder.RegisterType<IngestionEndpoints>().As<IMapEndpoints>();

        if (_config.Forwarder.Diagnostics.ExposeIngestionLog)
        {
            Log.Warning("Configured to expose ingestion log via HTTP API");
            builder.RegisterType<IngestionLogEndpoints>().As<IMapEndpoints>();

            var ingestionLogTemplate = "[{@t:o} {@l:u3}] {@m}\n";
            if (_config.Forwarder.Diagnostics.IngestionLogShowDetail)
            {
                Log.Warning("Including full client, payload, and error detail in the ingestion log");
                ingestionLogTemplate +=
                    "{#if ClientHostIP is not null}Client IP address: {ClientHostIP}\n{#end}" +
                    "{#if DocumentStart is not null}First {StartToLog} characters of payload: {DocumentStart:l}\n{#end}" +
                    "{@x}";
            }
            
            builder.Register(_ => new ExpressionTemplate(ingestionLogTemplate)).As<ITextFormatter>();
        }

        builder.Register(c =>
        {
            var config = c.Resolve<SeqCliConfig>();
            var baseUri = config.Connection.ServerUrl;
            if (string.IsNullOrWhiteSpace(baseUri))
                throw new ArgumentException("The destination Seq server URL must be configured in `SeqCli.json`.");

            if (!baseUri.EndsWith('/'))
                baseUri += '/';

            // additional configuration options that require the use of SocketsHttpHandler should be added to
            // this expression, using an "or" operator.

            var hasSocketHandlerOption =
                config.Connection.PooledConnectionLifetimeMilliseconds.HasValue;

            if (hasSocketHandlerOption)
            {
                var httpMessageHandler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = config.Connection.PooledConnectionLifetimeMilliseconds.HasValue ? TimeSpan.FromMilliseconds(config.Connection.PooledConnectionLifetimeMilliseconds.Value) : Timeout.InfiniteTimeSpan,
                };

                return new HttpClient(httpMessageHandler) { BaseAddress = new Uri(baseUri) };
            }

            return new HttpClient { BaseAddress = new Uri(baseUri) };
        }).SingleInstance();

        builder.RegisterInstance(_config);
    }
}