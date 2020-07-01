using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamLoco
{
    public class Client : IDisposable
    {
        readonly SessionCallbackDistributor callbackDistributor;
        readonly PacketDistributor packetDistributor;

        readonly Connection connection;
        readonly ConnectionEstablisherClient establisher;
        readonly SteamLocoTransport transport;
        readonly Queue<ReceivedPacket> receiveQueue = new Queue<ReceivedPacket>();

        readonly Thread thread;

        public bool Connected { get; private set; }
        public bool Closed { get; private set; }

        public Client(SteamLocoTransport transport, CSteamID remote, IList<EP2PSend> channels, TimeSpan connectTimeout)
        {
            this.transport = transport;

            callbackDistributor = new SessionCallbackDistributor();
            packetDistributor = new PacketDistributor(channels);
            connection = new Connection(callbackDistributor, packetDistributor, channels, remote);
            establisher = new ConnectionEstablisherClient(connection, connectTimeout);

            callbackDistributor.ConnectRequested += OnConnectRequested;

            establisher.Connected += OnConnected;
            establisher.Disconnected += OnDisconnected;

            // handle establisher's timeout

            thread = new Thread(NativeUpdate);
            thread.Start();
        }

        void OnConnected()
        {
            Connected = true;
            transport.OnClientConnected.Invoke();
        }

        void OnDisconnected()
        {
            Connected = false;
            transport.OnClientDisconnected.Invoke();
        }

        void OnConnectRequested(P2PSessionRequest_t ev)
        {
            if (connection.Remote == ev.m_steamIDRemote)
            {
                SteamNetworking.AcceptP2PSessionWithUser(ev.m_steamIDRemote);
            }
        }

        public void UnityUpdate()
        {
            establisher.Update();

            if (connection.Disconnected)
            {
                transport.OnClientDisconnected.Invoke();
                Dispose();
                return;
            }
            else
            {
                connection.ReceivePackets(receiveQueue);
            }

            while (receiveQueue.Count > 0)
            {
                var packet = receiveQueue.Dequeue();
                transport.OnClientDataReceived.Invoke(new ArraySegment<byte>(packet.Bytes), packet.ChannelID);
            }
        }

        void NativeUpdate()
        {
            while (true)
            {
                try
                {
                    packetDistributor.ReceivePacketsFromNetwork();
                    connection.SendPacketsInQueue();
                    Thread.Sleep(5);
                }
                catch (Exception e) when (!(e is ThreadAbortException))
                {
                    Debug.LogException(e);
                }
            }
        }

        public void Send(int channelID, ArraySegment<byte> segment)
        {
            connection.AddToSendQueue(channelID, segment);
        }

        public void Dispose()
        {
            if (Closed) return;

            Connected = false;
            Closed = true;

            thread.Abort();
            thread.Join();

            establisher.SendDisconnect();
            establisher.Connected -= OnConnected;
            callbackDistributor.Dispose();
            connection.Dispose();

            _ = WaitAndClose(connection.Remote);
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
