using System;
using System.Collections.Generic;

namespace SteamLoco
{
    public class ConnectionEstablisherClient
    {
        readonly Connection connection;
        readonly Queue<ReceivedPacket> receiveQueue = new Queue<ReceivedPacket>();

        public event Action Connected;
        public event Action Disconnected;
        public bool IsConnected { get; private set; }
        public bool IsDisconnected { get; private set; }
        readonly DateTime timeoutAt;
        DateTime lastSentAt;

        public ConnectionEstablisherClient(Connection connection, TimeSpan timeout)
        {
            this.connection = connection;
            timeoutAt = DateTime.UtcNow + timeout;
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
                if (DateTime.UtcNow > timeoutAt)
                {
                    IsDisconnected = true;
                    Disconnected?.Invoke();
                    return;
                }

                SendSyn();
                ReceiveSynAckAndRespond();
            }
        }

        void SendSyn()
        {
            var now = DateTime.UtcNow;
            if (now - lastSentAt > TimeSpan.FromSeconds(0.5))
            {
                connection.AddToSendQueue(connection.SystemChannelID, new[] { (byte)SystemPayloads.Syn });
                lastSentAt = now;
            }
        }

        void ReceiveSynAckAndRespond()
        {
            if (ConnectionEstablisherUtil.ExpectedPayloadReceived(connection, receiveQueue, SystemPayloads.Syn | SystemPayloads.Ack))
            {
                IsConnected = true;
                Connected?.Invoke();
                connection.AddToSendQueue(connection.SystemChannelID, new[] { (byte)SystemPayloads.Ack });
            }
        }

        void ReceiveDisconnect()
        {
            if (ConnectionEstablisherUtil.ExpectedPayloadReceived(connection, receiveQueue, SystemPayloads.Disconnect))
            {
                IsConnected = false;
                IsDisconnected = true;
                Disconnected?.Invoke();
            }
        }

        public void SendDisconnect()
        {
            connection.AddToSendQueue(connection.SystemChannelID, new[] { (byte)(SystemPayloads.Disconnect) });
            connection.SendPacketsInQueue();
        }
    }
}
