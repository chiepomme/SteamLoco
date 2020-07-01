using System;

namespace SteamLoco
{
    [Flags]
    public enum SystemPayloads : byte
    {
        Syn = 0b001,
        Ack = 0b010,
        Disconnect = 0b100,
    }
}
