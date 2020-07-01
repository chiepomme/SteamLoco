using System;
using System.Collections.Generic;

namespace SteamLoco
{
    public class ConnectionEstablisherServer
    {
        readonly Connection connection;
        readonly Queue<ReceivedPacket> receiveQueue = new Queue<ReceivedPacket>();

        DateTime lastSentAt;
        bool synReceived;

        public event Action<Connection> Connected;
        public event Action<Connection> Disconnected;

        public bool IsConnected { get; private set; }
        public bool IsDisconnected { get; private set; }

        public ConnectionEstablisherServer(Connection connection)
        {
            this.connection = connection;
        }

        public void Update()
        {
            if (IsDisconnected) return;

            if (IsConnected)
            {
                ReceiveDisconnect();
            }
            else
            {
                if (synReceived)
                {
                    SendSynAck();
                    ReceiveAck();
                }
                else
                {
                    ReceiveSyn();
                }
            }
        }

        void SendSynAck()
        {
            var now = DateTime.UtcNow;
            if (now - lastSentAt > TimeSpan.FromSeconds(0.5))
            {
                connection.AddToSendQueue(connection.SystemChannelID, new[] { (byte)(SystemPayloads.Syn | SystemPayloads.Ack) });
                lastSentAt = now;
            }
        }

        void ReceiveSyn()
        {
            if (ConnectionEstablisherUtil.ExpectedPayloadReceived(connection, receiveQueue, SystemPayloads.Syn))
            {
                synReceived = true;
            }
        }

        void ReceiveAck()
        {
            if (ConnectionEstablisherUtil.ExpectedPayloadReceived(connection, receiveQueue, SystemPayloads.Ack))
            {
                IsConnected = true;
                Connected?.Invoke(connection);
            }
        }

        void ReceiveDisconnect()
        {
            if (ConnectionEstablisherUtil.ExpectedPayloadReceived(connection, receiveQueue, SystemPayloads.Disconnect))
            {
                IsConnected = false;
                IsDisconnected = true;
                Disconnected?.Invoke(connection);
            }
        }

        public void SendDisconnect()
        {
            connection.AddToSendQueue(connection.SystemChannelID, new[] { (byte)(SystemPayloads.Disconnect) });
            connection.SendPacketsInQueue();
        }
    }
}
