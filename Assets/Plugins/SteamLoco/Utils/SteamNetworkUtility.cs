using Steamworks;
using System;

namespace SteamLoco
{
    public static class SteamNetworkUtility
    {
        public static int GetMaxPacketSize(EP2PSend reliability)
        {
            // https://partner.steamgames.com/doc/api/ISteamNetworking#EP2PSend
            switch (reliability)
            {
                case EP2PSend.k_EP2PSendUnreliable:
                case EP2PSend.k_EP2PSendUnreliableNoDelay:
                    return 1_200;
                case EP2PSend.k_EP2PSendReliable:
                case EP2PSend.k_EP2PSendReliableWithBuffering:
                    return 1_048_576;
            }

            throw new Exception($"EP2PSend {reliability} is not supported.");
        }
    }
}
