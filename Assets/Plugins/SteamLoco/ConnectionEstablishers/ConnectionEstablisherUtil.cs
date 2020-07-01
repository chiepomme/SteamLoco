using System.Collections.Generic;

namespace SteamLoco
{
    public static class ConnectionEstablisherUtil
    {
        public static bool ExpectedPayloadReceived(Connection connection, Queue<ReceivedPacket> cachedQueue, SystemPayloads expectedPayload)
        {
            connection.ReceiveSystemPackets(cachedQueue);
            while (cachedQueue.Count > 0)
            {
                var bytes = cachedQueue.Dequeue().Bytes;
                if (bytes.Length != 1) continue;

                var payload = (SystemPayloads)bytes[0];
                if (payload == expectedPayload)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
