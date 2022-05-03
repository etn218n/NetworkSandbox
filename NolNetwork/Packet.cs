using MessagePack;

namespace NolNetwork
{
    public enum PacketType
    {
        Ping,
        Pong,
        Disconnect,
        Message
    }

    public enum DisconnectionReason
    {
        Unreachable,
        Terminate
    }
    
    [MessagePackObject]
    public class Packet
    {
        [Key(0)] public PacketType Type;
        [Key(1)] public byte[] Payload;

        public Packet() { }
        
        public Packet(PacketType type, byte[] payload = null)
        {
            Type    = type;
            Payload = payload;
        }
    }
}