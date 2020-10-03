using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace SeqCli.Tests.Integration.Support
{
    public class TestConfiguration
    {
        public int ServerListenPort { get; }
        static readonly List<int> _usedPorts = new List<int>(); 

        public string ServerListenUrl => $"http://localhost:{ServerListenPort}";

        string EquivalentBaseDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory
            .Replace(Path.Combine("test", "SeqCli.Tests"), Path.Combine("src", "SeqCli"));

        public string TestedBinary => Path.Combine(EquivalentBaseDirectory, "seqcli.dll");


        public TestConfiguration()
        {
            ServerListenPort = GetAvailablePort(9989);
        }
        
        public CaptiveProcess SpawnCliProcess(string command, string additionalArgs = null, Dictionary<string, string> environment = null, bool skipServerArg = false)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var commandWithArgs = $"{command} {additionalArgs}";
            if (!skipServerArg)
                commandWithArgs += $" --server=\"{ServerListenUrl}\"";
            
            var args = $"{TestedBinary} {commandWithArgs}";
            return new CaptiveProcess("dotnet", args, environment);
        }
        
        public CaptiveProcess SpawnServerProcess(string storagePath)
        {
            if (storagePath == null) throw new ArgumentNullException(nameof(storagePath));

            var commandWithArgs = $"run --listen=\"{ServerListenUrl}\" --storage=\"{storagePath}\"";
            if (Environment.GetEnvironmentVariable("ENDTOEND_USE_DOCKER_SEQ") == "Y")
            {
                return new CaptiveProcess("docker", $"run --name seq-{ServerListenPort} -it --rm -e ACCEPT_EULA=Y -p {ServerListenPort}:80 datalust/seq:latest", stopCommandFullExePath: "docker", stopCommandArgs: $"stop seq-{ServerListenPort}");
            }
            return new CaptiveProcess("seq", commandWithArgs);
        }

        /// <summary>
        /// checks for used ports and retrieves the first free port
        /// </summary>
        /// <returns>the free port or 0 if it did not find a free port</returns>
        static int GetAvailablePort(int startingPort)
        {
            IPEndPoint[] endPoints;
            List<int> portArray = new List<int>();

            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

            //getting active connections
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            portArray.AddRange(from n in connections
                where n.LocalEndPoint.Port >= startingPort
                select n.LocalEndPoint.Port);

            //getting active tcp listners - WCF service listening in tcp
            endPoints = properties.GetActiveTcpListeners();
            portArray.AddRange(from n in endPoints
                where n.Port >= startingPort
                select n.Port);

            //getting active udp listeners
            endPoints = properties.GetActiveUdpListeners();
            portArray.AddRange(from n in endPoints
                where n.Port >= startingPort
                select n.Port);

            portArray.Sort();

            for (int i = startingPort; i < UInt16.MaxValue; i++)
                lock (_usedPorts)
                {
                    if (!portArray.Contains(i) && !_usedPorts.Contains(i))
                    {
                        _usedPorts.Add(i);
                        return i;
                    }
                }

            throw new Exception("Unable to find port");
        }
    }
}
