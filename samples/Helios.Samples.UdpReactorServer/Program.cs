﻿using System;
using System.Linq;
using System.Net;
using System.Text;
using Helios.Concurrency;
using Helios.Net;
using Helios.Reactor;
using Helios.Reactor.Udp;
using Helios.Topology;

namespace Helios.Samples.UdpReactorServer
{
    class Program
    {
        private const int DEFAULT_PORT = 1999;

        private static int Port;

        static void ServerPrint(INode node, string message)
        {
            Console.WriteLine("[{0}] {1}:{2}: {3}", DateTime.UtcNow, node.Host, node.Port, message);
        }

        static void Main(string[] args)
        {
            Port = args.Length < 1 ? DEFAULT_PORT : Int32.Parse(args[0]);
            var ip = IPAddress.Any;

            Console.WriteLine("Starting UDP echo server...");
            Console.WriteLine("Will begin listening for requests on {0}:{1}", ip, Port);
            IConnectionlessReactor reactor = new SimpleUdpReactor(ip, Port, FiberFactory.CreateFiber(1));
            reactor.DataAvailable += (sender, bytes) =>
            {
                var connection = bytes.ResponseChannel;
                var node = bytes.Data.RemoteHost;
                var cleanBuffer = bytes.Data.Buffer;
                var str = Encoding.UTF8.GetString(cleanBuffer.Take(bytes.Data.Length).ToArray()).Trim();
                if(str.ToLowerInvariant().Equals("close"))
                    reactor.Stop();
                ServerPrint(connection.Node, string.Format("recieved \"{0}\"", str));
                ServerPrint(connection.Node,
                    string.Format("sending \"{0}\" back to {1}:{2}", str, node.Host, node.Port));
                var sendBytes = Encoding.UTF8.GetBytes(str + Environment.NewLine);
                connection.Send(NetworkData.Create(node, sendBytes, sendBytes.Length));
            };
            reactor.Start();
        }
    }
}
