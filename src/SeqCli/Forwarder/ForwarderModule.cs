// Copyright Datalust Pty Ltd
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
using SeqCli.Config;
using SeqCli.Encryptor;
using SeqCli.Forwarder.Multiplexing;
using SeqCli.Forwarder.Web.Api;
using SeqCli.Forwarder.Web.Host;
using Serilog.Formatting.Display;

namespace SeqCli.Forwarder;

class ForwarderModule : Module
{
    readonly string _bufferPath;
    readonly SeqCliConfig _config;

    public ForwarderModule(string bufferPath, SeqCliConfig config)
    {
        _bufferPath = bufferPath ?? throw new ArgumentNullException(nameof(bufferPath));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ServerService>().SingleInstance();
        builder.RegisterType<ActiveLogBufferMap>()
            .WithParameter("bufferPath", _bufferPath)
            .SingleInstance();

        builder.RegisterType<HttpLogShipperFactory>().As<ILogShipperFactory>();
        builder.RegisterType<ServerResponseProxy>().SingleInstance();
        builder.RegisterType<ApiRoot>();
        builder.RegisterInstance(new MessageTemplateTextFormatter(
            "[{Timestamp:o} {Level:u3}] {Message}{NewLine}" + (_config.Forwarder.Diagnostics.IngestionLogShowDetail
                ? ""
                : "Client IP address: {ClientHostIP}{NewLine}First {StartToLog} characters of payload: {DocumentStart:l}{NewLine}{Exception}{NewLine}"))).SingleInstance();

        builder.Register(c =>
        {
            var config = c.Resolve<SeqCliConfig>();
            var baseUri = config.Connection.ServerUrl;
            if (string.IsNullOrWhiteSpace(baseUri))
                throw new ArgumentException("The destination Seq server URL must be configured in SeqForwarder.json.");

            if (!baseUri.EndsWith("/"))
                baseUri += "/";

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