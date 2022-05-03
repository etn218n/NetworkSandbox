using System;
using System.Text;

namespace NolNetwork
{
    public class Client
    {
        private Connection connectionsToServer;

        public void Connect(string ipAddress, int port)
        {
            connectionsToServer = new Connection();
            connectionsToServer.Connect(ipAddress, port);

            connectionsToServer.WhenConnected    += LogSuccessfulConnection;
            connectionsToServer.WhenRefused      += LogFailedConnection;
            connectionsToServer.WhenDisconnected += LogDisconnectionStatus;
            
            Console.WriteLine("[Client]:" + $"Try connect to {ipAddress}:{port}");
        }

        public void SendMessage(string message)
        {
            if (connectionsToServer == null)
                return;
            
            connectionsToServer.SendMessage(message);
        }

        public void Disconnect()
        {
            if (connectionsToServer == null)
                return;
            
            connectionsToServer.SendDisconnectSignal();
        }

        public void Update()
        {
            if (connectionsToServer.HasPendingPacket)
                HandlePacket(connectionsToServer.RetrieveNextPacket());
        }
        
        public void Shutdown()
        {
            if (connectionsToServer == null)
                return;
            
            connectionsToServer.Disconnect(DisconnectionReason.Terminate);
        }

        private void HandlePacket(Packet packet)
        {
            switch (packet.Type)
            {
                case PacketType.Message: Console.WriteLine("[Server]:" + Encoding.UTF8.GetString(packet.Payload)); break;
            }
        }

        private void LogSuccessfulConnection()
        {
            Console.WriteLine("[Client]:" + "Connection accepted");
        }
        
        private void LogFailedConnection()
        {
            Console.WriteLine("[Client]:" + "Connection refused");
        }
        
        private void LogDisconnectionStatus(DisconnectionReason reason)
        {
            switch (reason)
            {
                case DisconnectionReason.Terminate:   Console.WriteLine("[Client]:" + "Disconnected (Reason: Graceful exit)"); break;
                case DisconnectionReason.Unreachable: Console.WriteLine("[Client]:" + "Disconnected (Reason: Host shutdown)"); break;
                
                default: throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
            }
        }
    }
}