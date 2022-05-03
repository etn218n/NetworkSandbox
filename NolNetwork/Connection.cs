using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NolNetwork
{
    public class Connection
    {
        private Socket socket;
        private IPEndPoint endPoint;
        private Timer checkPingScheduler;

        private int id;
        private int timeout;
        private int maxPacketSize;
        private int maxBufferSize;
        private int totalReceivedByteCount;
        private int rtt;
        private bool isActive;
        private byte[] receiveBuffer;
        private Queue<Packet> pendingPackets;
        private Queue<Packet> pongPackets;

        private DateTime lastPingTimeStamp;

        public event Action WhenRefused; 
        public event Action WhenConnected; 
        public event Action<DisconnectionReason> WhenDisconnected;

        public int Id => id;
        public int MaxPacketSize => maxPacketSize;
        public int MaxBufferSize => maxBufferSize;
        public bool IsActive => isActive;
        public bool HasPendingPacket => pendingPackets.Count > 0;
        public float Rtt => rtt;

        public Connection(Socket socket = null, int id = -1, int timeout = 1000, int maxPacketSize = 4, int maxBufferSize = 4096)
        {
            this.id             = id;
            this.socket         = socket;
            this.timeout        = timeout;
            this.maxPacketSize  = maxPacketSize;
            this.maxBufferSize  = maxBufferSize;
            this.receiveBuffer  = new byte[maxBufferSize];
            this.pendingPackets = new Queue<Packet>();
            this.pongPackets    = new Queue<Packet>();
        }

        public void Connect(string ipAddress, int port)
        {
            if (socket != null && socket.Connected)
                return;
            
            var hostEntry = Dns.GetHostEntry(ipAddress);
            
            endPoint = new IPEndPoint(hostEntry.AddressList[0], port);
            socket   = new Socket(hostEntry.AddressList[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            socket.BeginConnect(endPoint, ConnectCallback, socket);
        }

        public void Disconnect(DisconnectionReason reason)
        {
            if (!isActive)
                return;
            
            if (socket.Connected)
                socket.Shutdown(SocketShutdown.Both);

            socket.Close();
            checkPingScheduler.Dispose();
            isActive = false;
            WhenDisconnected?.Invoke(reason);
        }

        public void SendMessage(string message)
        {
            if (!socket.Connected)
                return;

            var packet = PacketFactory.CreateMessagePacket(message);
            var bytes  = PacketFactory.PacketToBytes(packet);

            socket.Send(bytes);
        }

        public void SendDisconnectSignal()
        {
            if (!socket.Connected)
                return;

            var packet = PacketFactory.CreateDisconnectPacket();
            var bytes  = PacketFactory.PacketToBytes(packet);

            socket.Send(bytes);
        }

        public void BeginReceive()
        {
            if (!socket.Connected)
                return;
            
            socket.BeginReceive(receiveBuffer, 0, maxPacketSize, SocketFlags.None, ReceiveCallback, socket);
            isActive = true;
            
            Ping();
            SchedulePingCheck();
        }

        private void ConnectCallback(IAsyncResult asyncResult)
        {
            if (asyncResult.AsyncState is Socket handlerSocket)
            {
                if (!handlerSocket.Connected)
                {
                    WhenRefused?.Invoke();
                    return;
                }
                
                BeginReceive();
                WhenConnected?.Invoke();
            }
        }

        private void SchedulePingCheck()
        {
            checkPingScheduler = new Timer(CheckPongPacket, null, timeout, timeout - 100);
        }
        
        private void Ping()
        {
            if (!socket.Connected)
                return;

            var packet = PacketFactory.CreatePingPacket();
            var bytes  = PacketFactory.PacketToBytes(packet);

            socket.Send(bytes);
            lastPingTimeStamp = DateTime.Now;
        }
        
        private void Pong()
        {
            if (!socket.Connected)
                return;

            var packet = PacketFactory.CreatePongPacket();
            var bytes  = PacketFactory.PacketToBytes(packet);

            socket.Send(bytes);
        }

        private void CheckPongPacket(object state)
        {
            if (pongPackets.Count <= 0)
            {
                Disconnect(DisconnectionReason.Unreachable);
                return;
            }
            
            pongPackets.Dequeue();
            Ping();
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            if (asyncResult.AsyncState is Socket handler)
            {
                try
                {
                    var receivedByteCount = handler.EndReceive(asyncResult);
                    ReceivePacket(receivedByteCount);
                }
                catch
                {
                    Disconnect(DisconnectionReason.Unreachable);
                }
            }
        }

        private void ReceivePacket(int receivedByteCount)
        {
            if (receivedByteCount == 0)
            {
                BeginReceive();
                return;
            }
                
            var payloadSize = BinaryPrimitives.ReadInt32LittleEndian(receiveBuffer);
            var headerSize  = sizeof(int);

            totalReceivedByteCount += receivedByteCount;
                
            // packet is fully received
            if (totalReceivedByteCount >= payloadSize + headerSize)
            {
                totalReceivedByteCount = Math.Clamp(totalReceivedByteCount - payloadSize - headerSize, 0, maxBufferSize);
                    
                ExtractPacket(receiveBuffer, payloadSize, headerSize);
                RemovePacketFromBuffer(receiveBuffer, payloadSize, headerSize, totalReceivedByteCount);
            }
                
            socket.BeginReceive(receiveBuffer, totalReceivedByteCount, maxPacketSize, SocketFlags.None, ReceiveCallback, socket);
        }

        private void ExtractPacket(byte[] buffer, int payloadSize, int headerSize)
        {
            var payload = new byte[payloadSize];
                    
            Array.Copy(buffer, headerSize, payload, 0, payloadSize);

            var packet = PacketFactory.Deserialize(payload);

            switch (packet.Type)
            {
                case PacketType.Ping: Pong(); break;
                case PacketType.Pong: RecordPong(packet); break;
                default: pendingPackets.Enqueue(packet); break;
            }
        }

        private void RecordPong(Packet packet)
        {
            pongPackets.Enqueue(packet);
            rtt = (DateTime.Now - lastPingTimeStamp).Milliseconds;
            Console.WriteLine($"Ping = {rtt} ms");
        }

        private void RemovePacketFromBuffer(byte[] buffer, int payloadSize, int headerSize, int leftOverBytes)
        {
            if (leftOverBytes <= 0)
                return;
            
            var temp = new byte[leftOverBytes];

            Array.Copy(buffer, payloadSize + headerSize, temp, 0, leftOverBytes);
            Array.Copy(temp, buffer, leftOverBytes);
        }

        public Packet RetrieveNextPacket()
        {
            return pendingPackets.Dequeue();
        }
    }
}