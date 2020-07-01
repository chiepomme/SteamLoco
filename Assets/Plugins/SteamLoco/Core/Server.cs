using Steamworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamLoco
{
    public class Server : IDisposable
    {
        readonly SteamLocoTransport transport;

        readonly SessionCallbackDistributor callbackDistributor = new SessionCallbackDistributor();
        readonly PacketDistributor packetDistributor;

        readonly Queue<ReceivedPacket> receiveQueue = new Queue<ReceivedPacket>();
        readonly ConcurrentDictionary<int, Connection> connections = new ConcurrentDictionary<int, Connection>();
        readonly ConcurrentDictionary<int, ConnectionEstablisherServer> establishers = new ConcurrentDictionary<int, ConnectionEstablisherServer>();

        readonly List<CSteamID> connectionIDToSteamID = new List<CSteamID>();
        readonly Dictionary<CSteamID, int> steamIDToConnectionID = new Dictionary<CSteamID, int>();

        readonly IList<EP2PSend> channels;
        readonly Thread thread;

        public bool Closed { get; private set; }

        public Server(SteamLocoTransport transport, IList<EP2PSend> channels)
        {
            this.transport = transport;
            this.channels = channels;

            packetDistributor = new PacketDistributor(channels);
            packetDistributor.PacketReceived += OnPacketReceived;
            callbackDistributor.ConnectRequested += OnConnectRequest;

            var steamID = SteamUser.GetSteamID();
            connectionIDToSteamID.Add(steamID);
            steamIDToConnectionID.Add(steamID, connectionIDToSteamID.Count - 1);

            thread = new Thread(NativeUpdate);
            thread.Start();
        }

        void OnPacketReceived(ReceivedPacket packet)
        {
            if (steamIDToConnectionID.ContainsKey(packet.Sender)) return;

            var connectionID = steamIDToConnectionID.Count;
            connectionIDToSteamID.Add(packet.Sender);
            steamIDToConnectionID.Add(packet.Sender, connectionID);

            var connection = new Connection(callbackDistributor, packetDistributor, channels, packet.Sender);
            // FIXME: 初回必ず SYN を落としてしまう
            var establisher = new ConnectionEstablisherServer(connection);
            establisher.Connected += OnConnected;
            establisher.Disconnected += OnDisconnected;

            if (!connections.TryAdd(connectionID, connection)) Debug.LogError("Already connected");
            if (!establishers.TryAdd(connectionID, establisher)) Debug.LogError("Already connected");
        }

        void OnConnectRequest(P2PSessionRequest_t ev)
        {
            // TODO: Check lobby members
            SteamNetworking.AcceptP2PSessionWithUser(ev.m_steamIDRemote);
        }

        void OnConnected(Connection connection)
        {
            transport.OnServerConnected.Invoke(steamIDToConnectionID[connection.Remote]);
        }

        void OnDisconnected(Connection connection)
        {
            Debug.Log($"Disconnect #{steamIDToConnectionID[connection.Remote]} by client disconnect message");
            transport.OnServerDisconnected.Invoke(steamIDToConnectionID[connection.Remote]);
        }

        public void UnityUpdate()
        {
            foreach (var kvp in establishers)
            {
                kvp.Value.Update();
            }

            foreach (var kvp in connections)
            {
                var connection = kvp.Value;
                connection.ReceivePackets(receiveQueue);
                if (connection.Disconnected)
                {
                    Debug.Log($"Connection #{kvp.Key} is disconnected by steam event");
                    transport.OnServerDisconnected.Invoke(kvp.Key);
                }
            }

            while (receiveQueue.Count > 0)
            {
                var packet = receiveQueue.Dequeue();
                var sender = steamIDToConnectionID[packet.Sender];
                transport.OnServerDataReceived.Invoke(sender, new ArraySegment<byte>(packet.Bytes), packet.ChannelID);
            }
        }

        void NativeUpdate()
        {
            while (true)
            {
                try
                {
                    packetDistributor.ReceivePacketsFromNetwork();

                    foreach (var kvp in connections)
                    {
                        kvp.Value.SendPacketsInQueue();
                    }
                }
                catch (Exception e) when (!(e is ThreadAbortException))
                {
                    Debug.LogException(e);
                }

                Thread.Sleep(5);
            }
        }

        public bool DisconnectClient(int connectionID)
        {
            Debug.Log($"Disconnect #{connectionID} by server explicitly");
            if (connections.TryRemove(connectionID, out var connection))
            {
                if (establishers.TryRemove(connectionID, out var establisher))
                {
                    establisher.SendDisconnect();
                }

                steamIDToConnectionID.Remove(connection.Remote);
                connection.Dispose();
                Debug.Log($"Connection #{connectionID} disposed");

                _ = WaitAndClose(connection.Remote);

                return true;
            }

            return false;
        }

        public string GetClientAddress(int connectionId)
        {
            return connectionIDToSteamID[connectionId].ToString();
        }

        public bool Send(List<int> connectionIDs, int channelID, ArraySegment<byte> segment)
        {
            var bytes = ByteArrayUtil.ToBytes(segment);

            foreach (var connectionID in connectionIDs)
            {
                var connection = connections[connectionID];
                connection.AddToSendQueue(channelID, bytes);
            }

            return true;
        }

        public void Dispose()
        {
            if (Closed) return;
            Closed = true;

            foreach (var connectionID in connections.Keys)
            {
                DisconnectClient(connectionID);
            }

            thread.Abort();
            thread.Join();

            callbackDistributor.ConnectRequested -= OnConnectRequest;
            callbackDistributor.Dispose();
            packetDistributor.PacketReceived -= OnPacketReceived;
        }

        async Task WaitAndClose(CSteamID remote)
        {
            await Task.Delay(2000);
            if (SteamManager.Initialized)
            {
                SteamNetworking.CloseP2PSessionWithUser(remote);
                Debug.Log($"SteamConnection Closed {remote}");
            }
        }
    }
}
