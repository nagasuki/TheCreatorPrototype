using System;
using System.Collections.Generic;
using FiveMinuteChat.Helpers;
using FiveMinutes.Model;
using FiveMinutes.Model.Messages;
using FiveMinutes.Model.Messages.Client;
using FiveMinutes.Model.Messages.Server;

namespace FiveMinuteChat.Interfaces
{
    public delegate void OnConnectionAccepted(List<ChannelInfo> availableChannels);
    public delegate void OnChannelJoined(ChannelInfo channelInfo, bool canLeave, bool isSilenced);
    public delegate void OnChannelLeft(string channelName);
    public delegate void OnChannelInfo(ChannelInfo channelInfo);
    
    public interface IConnector
    {
        event OnChannelJoined ChannelJoined;
        event OnChannelLeft ChannelLeft;
        event OnConnectionAccepted ConnectionAccepted;
        event OnChannelInfo ChannelInfoReceived;
        void InitWithMessageHandler( MessageHandlerBase messageHandler );
        void On( MessageBase message );
        Guid Subscribe<T>( Action<T> callback ) where T : MessageBase;
        void Unsubscribe( Guid callbackId );
        void Connect(string backendEndpoint, int backendPort);
        void SetUsername( string username );
        void Send(ClientMessageBase message, bool shouldEncrypt = true);
        bool Connected { get; }
        void Reconnect();
        void Disconnect( bool allowReconnect );
        void AckMessage( Guid messageId );
        void SetCredentials(string applicationName, string applicationSecret, string uniqueUserId);
        void OnChannelJoined( ChannelInfo channelInfo, bool canLeave, bool isSilenced );
        void OnChannelLeft( string channelName );
        void OnConnectionAccepted( List<ChannelInfo> welcomeAvailableChannels );
        void OnChannelInfoReceived( ChannelInfo channelInfo );
    }
}
