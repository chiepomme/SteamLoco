using Steamworks;
using System;
using System.Collections.Generic;

namespace SteamLoco
{
    public class PacketDistributor
    {
        readonly IList<EP2PSend> channels;

        public event Action<ReceivedPacket> PacketReceived;

        public PacketDistributor(IList<EP2PSend> channels)
        {
            this.channels = channels;
        }

        public void ReceivePacketsFromNetwork()
        {
            for (var channelID = 0; channelID < channels.Count; channelID++)
            {
                while (SteamNetworking.IsP2PPacketAvailable(out var size, channelID))
                {
                    var bytes = new byte[size];
                    SteamNetworking.ReadP2PPacket(bytes, (uint)bytes.Length, out _, out var sender, channelID);
                    PacketReceived?.Invoke(new ReceivedPacket(sender, bytes, channelID));
                }
            }
        }
    }
}
