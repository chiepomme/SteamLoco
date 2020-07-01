using Mirror;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteamLoco
{
    public class SteamLocoTransport : Transport
    {
        Client client;
        Server server;

        [SerializeField]
        float connectTimeoutSec = 10;
        [SerializeField]
        List<EP2PSend> channels;

        void Awake()
        {
            channels.Add(EP2PSend.k_EP2PSendReliable);
        }

        public override bool Available() => SteamManager.Initialized;

        public override void ClientConnect(string address)
        {
            if (SteamManager.Initialized) { }

            var remote = (CSteamID)ulong.Parse(address);
            client = new Client(this, remote, channels, TimeSpan.FromSeconds(connectTimeoutSec));
        }

        public override bool ClientConnected() => client.Connected;
        public override void ClientDisconnect() => client?.Dispose();

        public override bool ClientSend(int channelId, ArraySegment<byte> segment)
        {
            client.Send(channelId, segment);
            return true;
        }

        public override int GetMaxPacketSize(int channelId = 0) => SteamNetworkUtility.GetMaxPacketSize(channels[channelId]);

        public override bool ServerActive() => server != null;

        public override bool ServerDisconnect(int connectionId) => server.DisconnectClient(connectionId);

        public override string ServerGetClientAddress(int connectionId) => server.GetClientAddress(connectionId);

        public override bool ServerSend(List<int> connectionIds, int channelId, ArraySegment<byte> segment)
            => server.Send(connectionIds, channelId, segment);

        public override void ServerStart()
        {
            if (SteamManager.Initialized) { }
            server = new Server(this, channels);
        }

        public override void ServerStop()
        {
            server.Dispose();
        }

        public override Uri ServerUri()
        {
            throw new NotImplementedException();
        }

        void LateUpdate()
        {
            client?.UnityUpdate();
            server?.UnityUpdate();
        }

        public override void Shutdown()
        {
            server?.Dispose();
            server = null;
            client?.Dispose();
            client = null;
        }
    }
}
