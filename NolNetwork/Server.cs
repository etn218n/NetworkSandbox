using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NolNetwork
{
    public class Server
    {
        private readonly int port;
        private readonly string ipAddress;
        
        private int nextConnectionID;

        private Socket socket;
        private List<Connection> connectionsToClient;

        public Server(string ipAddress, int port)
        {
            this.port = port;
            this.ipAddress = ipAddress;
            this.connectionsToClient = new List<Connection>();
        }

        public void Start()
        {
            var hostEntry = Dns.GetHostEntry(ipAddress);
            var endPoint  = new IPEndPoint(hostEntry.AddressList[0], port);
            
            socket = new Socket(hostEntry.AddressList[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endPoint);
            socket.Listen();

            try
            {
                socket.BeginAccept(AcceptCallback, socket);
                Console.WriteLine("[Server]:" + "Start listening");
            }
            catch (Exception e)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Console.WriteLine(e);
            }
        }

        public void Update()
        {
            foreach (var connection in connectionsToClient)
            {
                if (connection.HasPendingPacket)
                    HandlePacket(connection.RetrieveNextPacket(), connection);
            }
        }
        
        public void Shutdown()
        {
            if (socket == null)
                return;
            
            socket.Close();
            Console.WriteLine("[Server]:" + "Shutdown");
        }

        private void HandlePacket(Packet packet, Connection connection)
        {
            switch (packet.Type)
            {
                case PacketType.Message: LogMessage(packet, connection); break;
                case PacketType.Disconnect: ShutdownConnection(connection); break;
            }
        }

        private void ShutdownConnection(Connection connection)
        {
            connection.Disconnect(DisconnectionReason.Terminate);
            connectionsToClient.Remove(connection);
            Console.WriteLine("[Server]:" + $"Client {connection.Id} disconnected.");
        }
        
        private void AcceptCallback(IAsyncResult asyncResult)
        {
            if (asyncResult.AsyncState is Socket serverSocket)
            {
                var clientSocket = serverSocket.EndAccept(asyncResult);
                var connection   = new Connection(clientSocket, RetrieveConnectionID());
                
                connectionsToClient.Add(connection);
                connection.BeginReceive();
                connection.SendMessage("Greeting!");
                connection.WhenDisconnected += reason => Console.WriteLine("[Server]:" + $"Client {connection.Id} disconnected");
                
                Console.WriteLine("[Server]:" + $"Client {connection.Id} connected");

                socket.BeginAccept(AcceptCallback, socket);
            }
        }

        private int RetrieveConnectionID()
        {
            return ++nextConnectionID;
        }
        
        private void LogMessage(Packet packet, Connection connection)
        {
            Console.WriteLine($"[Client {connection.Id}]:" + Encoding.UTF8.GetString(packet.Payload));
        }
    }
}