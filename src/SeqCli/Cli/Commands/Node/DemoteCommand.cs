using System;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api.Model.Cluster;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;

#nullable enable

namespace SeqCli.Cli.Commands.Node;

[Command("node", "demote", "Begin demotion of the current leader node", 
    Example = "seqcli node demote --verbose --wait")]
class DemoteCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly ConfirmFeature _confirm;
    bool _wait;

    public DemoteCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add("wait", "Wait for the leader to be demoted before exiting", _ => _wait = true);
        _confirm = Enable<ConfirmFeature>();
            
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        var connection = _connectionFactory.Connect(_connection);

        if (!_confirm.TryConfirm("This will demote the current cluster leader."))
        {
            await Console.Error.WriteLineAsync("Canceled by user.");
            return 1;
        }
            
        var demoting = (await connection.ClusterNodes.ListAsync()).SingleOrDefault(n => n.Role == NodeRole.Leader);

        if (demoting == null)
        {
            Log.Error("No cluster node is in the leader role");
            return 1;
        }
            
        await connection.ClusterNodes.DemoteAsync(demoting);

        if (!_wait)
        {
            Log.Information("Demotion of node {ClusterNodeId}/{ClusterNodeName} commenced", demoting.Id, demoting.Name);
            return 0;
        }

        var lastStatus = demoting.StateDescription;
        Log.Information("Waiting for demotion of node {ClusterNodeId}/{ClusterNodeName} to complete ({LeaderNodeStatus})", demoting.Id, demoting.Name, lastStatus);

        while (true)
        {
            await Task.Delay(100);
                
            var nodes = await connection.ClusterNodes.ListAsync();
            demoting = nodes.FirstOrDefault(n => n.Id == demoting.Id);

            if (demoting == null)
            {
                var newLeader = nodes.Single(n => n.Role == NodeRole.Leader);
                Log.Information("Demotion completed; old leader is out of service, new leader is {ClusterNodeId}/{ClusterNodeName}", newLeader.Id, newLeader.Name);
                return 0;
            }

            if (demoting.StateDescription != lastStatus)
            {
                lastStatus = demoting.StateDescription;
                Log.Information("Demotion in progress ({LeaderNodeStatus})", lastStatus);
            }
        }
    }
}