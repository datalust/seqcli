using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api.Model.AppInstances;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Signals;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.AppInstance;

[Command("appinstance", "create", "Create an instance of an installed app",
    Example = "seqcli appinstance create -t 'Email Ops' --app hostedapp-314159 -p To=ops@example.com")]
class CreateCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;

    string? _title, _appId, _streamIncomingEventsSignal;
    readonly Dictionary<string, string> _settings = new();
    readonly List<string> _overridable = new();
    bool _streamIncomingEvents;

    public CreateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "t=|title=",
            "A title for the app instance",
            t => _title = ArgumentString.Normalize(t));

        Options.Add(
            "app=",
            "The id of the installed app package to instantiate",
            app => _appId = ArgumentString.Normalize(app));

        Options.Add(
            "p={=}|property={=}",
            "Specify name/value settings for the app, e.g. `-p ToAddress=example@example.com -p Subject=\"Alert!\"`",
            (n, v) =>
            {
                var name = n.Trim();
                var valueText = v?.Trim();
                _settings.Add(name, valueText ?? "");
            });

        Options.Add(
            "stream",
            "Stream incoming events to this app instance as they're ingested",
            _ =>
            {
                _streamIncomingEvents = true;
            }
        );

        Options.Add(
            "stream-signal=",
            "Specify a signal expression to limit the events streamed to those matching the expression (implies --stream)",
            s =>
            {
                _streamIncomingEvents = true;
                _streamIncomingEventsSignal = s;
            });

        Options.Add(
            "overridable=",
            "Specify setting names that may be overridden by users when invoking the app",
            s => _overridable.Add(s));

        _connection = Enable<ConnectionFeature>();
        _output = Enable(new OutputFormatFeature(config.Output));
    }

    protected override async Task<int> Run()
    {
        var connection = _connectionFactory.Connect(_connection);
        
        AppInstanceEntity instance = await connection.AppInstances.TemplateAsync(_appId)!;

        bool ValidateSettingName(string settingName)
        {
            if (!instance.Settings!.ContainsKey(settingName))
            {
                Log.Error("The app does not accept a setting with name {SettingName}; available settings are: {AvailableSettings}", settingName, instance.Settings.Keys.ToArray());
                return false;
            }

            return true;
        }

        instance.Title = _title;
        instance.AcceptStreamedEvents = _streamIncomingEvents;
        instance.StreamedSignalExpression = !string.IsNullOrWhiteSpace(_streamIncomingEventsSignal) ? SignalExpressionParser.ParseExpression(_streamIncomingEventsSignal) : null;

        foreach (var setting in _settings)
        {
            if (!ValidateSettingName(setting.Key))
                return 1;
                
            instance.Settings![setting.Key] = setting.Value;
        }

        foreach (var overridable in _overridable)
        {
            if (!ValidateSettingName(overridable))
                return 1;
            
            instance.InvocationOverridableSettings!.Add(overridable);
        }
        
        instance = await connection.AppInstances.AddAsync(instance);

        _output.WriteEntity(instance);

        return 0;
    }
}