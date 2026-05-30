using System;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;

namespace SeqCli.EndToEnd.Mcp;

public class McpSessionBasicsTestCase: ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        // Log a handful of simple informational events through the logger. It's worth going to some effort to add
        // some nested structured properties.
        
        // Configure an `McpClient` connected to `seqcli mcp run` pointing to the shared Seq connection.
        
        // Call the search tool and verify that the events are (conditionally) found
        
        // Call the result inspection tool and pull back each event using the ids returned in the search results,
        // ensuring they're what we expect.
        
        // Call the schema tool and check that all of the property paths from all of the events are present.
        
        // Call the query tool and compute an aggregate over the events, making sure we get the expected result set.

        throw new NotImplementedException();
    }
}