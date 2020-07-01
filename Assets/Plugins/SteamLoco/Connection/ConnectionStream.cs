using Steamworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SteamLoco
{
    public class ConnectionStream : IDisposable
    {
        public readonly CSteamID Remote;
        public readonly int ChannelID;
        public readonly EP2PSend Reliability;

        readonly PacketDistributor packetDistributor;

        readonly ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();
        readonly ConcurrentQueue<ReceivedPacket> receiveQueue = new ConcurrentQueue<ReceivedPacket>();

        public ConnectionStream(PacketDistributor packetDistributor, CSteamID remote, int channelID, EP2PSend reliability)
        {
            Remote = remote;
            ChannelID = channelID;
            Reliability = reliability;

            this.packetDistributor = packetDistributor;
            packetDistributor.PacketReceived += OnPacketReceived;
        }

        public void AddToSendQueue(byte[] bytes)
        {
            sendQueue.Enqueue(bytes);
        }

        public void SendPacketsInQueue()
        {
            while (sendQueue.Count > 0)
            {
                if (sendQueue.TryDequeue(out var bytes))
                {
                    SteamNetworking.SendP2PPacket(Remote, bytes, (uint)bytes.Length, Reliability, ChannelID);
                }
            }
        }

        public void ReceivePackets(Queue<ReceivedPacket> queue)
        {
            while (receiveQueue.Count > 0)
            {
                if (receiveQueue.TryDequeue(out var bytes))
                {
                    queue.Enqueue(bytes);
                }
            }
        }

        void OnPacketReceived(ReceivedPacket packet)
        {
            if (packet.Sender == Remote && packet.ChannelID == ChannelID)
            {
                receiveQueue.Enqueue(packet);
            }
        }

        public void Dispose()
        {
            packetDistributor.PacketReceived -= OnPacketReceived;
        }
    }
}
