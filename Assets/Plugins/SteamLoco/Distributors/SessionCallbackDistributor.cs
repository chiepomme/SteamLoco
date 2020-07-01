using Steamworks;
using System;
using UnityEngine;

namespace SteamLoco
{
    public class SessionCallbackDistributor : IDisposable
    {
        readonly Callback<P2PSessionRequest_t> connectRequestCallback;
        readonly Callback<P2PSessionConnectFail_t> connectFailCallback;

        public event Action<P2PSessionRequest_t> ConnectRequested;
        public event Action<P2PSessionConnectFail_t> ConnectFailed;

        public SessionCallbackDistributor()
        {
            connectRequestCallback = Callback<P2PSessionRequest_t>.Create(OnConnectRequest);
            connectFailCallback = Callback<P2PSessionConnectFail_t>.Create(OnConnectionFail);
        }

        void OnConnectRequest(P2PSessionRequest_t ev)
        {
            ConnectRequested?.Invoke(ev);
        }

        void OnConnectionFail(P2PSessionConnectFail_t ev)
        {
            Debug.Log($"Connect Failed to {ev.m_steamIDRemote}");
            ConnectFailed?.Invoke(ev);
        }

        public void Dispose()
        {
            connectRequestCallback.Dispose();
            connectFailCallback.Dispose();
        }
    }
}
