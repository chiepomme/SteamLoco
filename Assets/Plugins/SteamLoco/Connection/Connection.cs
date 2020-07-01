using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SteamLoco
{
    public class Connection : IDisposable
    {
        public bool Disconnected { get; private set; }
        public int SystemChannelID { get; private set; }

        public readonly CSteamID Remote;

        readonly ConnectionStream[] allStreams;
        readonly ConnectionStream[] normalStreams;
        readonly ConnectionStream systemStream;

        readonly SessionCallbackDistributor callbackDistributor;

        public Connection(SessionCallbackDistributor callbackDistributor,
                               PacketDistributor packetDistributor,
                               IList<EP2PSend> channels, CSteamID remote)
        {
            Remote = remote;
            allStreams = channels.Select((c, i) => new ConnectionStream(packetDistributor, remote, i, c)).ToArray();
            normalStreams = allStreams.Take(channels.Count - 1).ToArray();
            systemStream = allStreams.Skip(channels.Count - 1).Single();
            SystemChannelID = channels.Count - 1;

            this.callbackDistributor = callbackDistributor;
            callbackDistributor.ConnectFailed += OnConnectFailed;
        }

        void OnConnectFailed(P2PSessionConnectFail_t ev)
        {
            if (ev.m_steamIDRemote == Remote)
            {
                Debug.Log($"Disconnected from {Remote}");
                Disconnected = true;
            }
        }

        public void AddToSendQueue(int channelID, byte[] bytes)
        {
            allStreams[channelID].AddToSendQueue(bytes);
        }

        public void AddToSendQueue(int channelID, ArraySegment<byte> segment)
        {
            AddToSendQueue(channelID, ByteArrayUtil.ToBytes(segment));
        }

        public void ReceivePackets(Queue<ReceivedPacket> queue)
        {
            foreach (var stream in normalStreams)
            {
                stream.ReceivePackets(queue);
            }
        }

        public void ReceiveSystemPackets(Queue<ReceivedPacket> queue)
        {
            systemStream.ReceivePackets(queue);
        }

        public void SendPacketsInQueue()
        {
            foreach (var stream in allStreams)
            {
                stream.SendPacketsInQueue();
            }
        }

        public void Dispose()
        {
            callbackDistributor.ConnectFailed -= OnConnectFailed;
        }
    }
}
