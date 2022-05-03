using System;
using System.Text;
using MessagePack;

namespace NolNetwork
{
    public static class PacketFactory
    {
        public static Packet CreatePingPacket()
        {
            return new Packet(PacketType.Ping);
        }
        
        public static Packet CreatePongPacket()
        {
            return new Packet(PacketType.Pong);
        }
        
        public static Packet CreateMessagePacket(string message)
        {
            return new Packet(PacketType.Message, Encoding.UTF8.GetBytes(message));
        }

        public static Packet CreateDisconnectPacket()
        {
            return new Packet(PacketType.Disconnect);
        }

        public static byte[] Serialize(Packet packet)
        {
            return MessagePackSerializer.Serialize(packet);
        }

        public static Packet Deserialize(byte[] bytes)
        {
            return MessagePackSerializer.Deserialize<Packet>(bytes);
        }
        
        public static byte[] PacketToBytes(Packet packet)
        {
            var packetBytes     = Serialize(packet);
            var packetSizeBytes = BitConverter.GetBytes(packetBytes.Length);
            var finalBytes      = new byte[packetSizeBytes.Length + packetBytes.Length];
            
            Array.Copy(packetSizeBytes, finalBytes, 4);
            Array.Copy(packetBytes, 0, finalBytes, packetSizeBytes.Length, packetBytes.Length);

            return finalBytes;
        }
    }
}