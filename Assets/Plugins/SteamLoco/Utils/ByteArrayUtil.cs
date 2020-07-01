using System;

namespace SteamLoco
{
    public static class ByteArrayUtil
    {
        public static byte[] ToBytes(ArraySegment<byte> segment)
        {
            var bytes = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, bytes, 0, segment.Count);
            return bytes;
        }
    }
}
