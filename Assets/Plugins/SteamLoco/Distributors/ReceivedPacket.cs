using Steamworks;

namespace SteamLoco
{
    public class ReceivedPacket
    {
        public readonly CSteamID Sender;
        public readonly byte[] Bytes;
        public readonly int ChannelID;

        public ReceivedPacket(CSteamID sender, byte[] bytes, int channelID)
        {
            Sender = sender;
            Bytes = bytes;
            ChannelID = channelID;
        }
    }
}
