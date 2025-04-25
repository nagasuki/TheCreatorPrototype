using System;
using System.Collections.Generic;
using System.Threading;
using FiveMinuteChat.Helpers;
using FiveMinuteChat.Interfaces;
using FiveMinutes.Helpers;
using FiveMinutes.Model;
using FiveMinutes.Model.Messages;
using FiveMinutes.Model.Messages.Client;
using FiveMinutes.Telepathy;
using UnityEngine;
using Timer = System.Timers.Timer;

namespace FiveMinuteChat.Connectors
{
    public class TcpConnector : ConnectorBase
    {
        public override bool Connected => _client.Connected;

        private Timer _heartbeatTimer;
        private string _backendEndpoint;
        private int _backendPort;        
        private string _username;
        private bool _shouldReconnect = true;
        private bool _isReconnecting;
        private readonly ClientContainer _client = new ();
        private Guid _connectionInitId = Guid.NewGuid();

        private class ClientContainer : IConnectorClient
        {
            public readonly Client Client = new Client();
            public bool Connected => Client.Connected;

            public void Connect( string backendEndpoint, int backendPort )
                => Client.Connect( backendEndpoint, backendPort );
            
            public void Disconnect() 
                => Client.Disconnect();

            public bool Send( MessageBase message, bool shouldEncrypt = true)
                => Client.Send( Serializer.Serialize(message), shouldEncrypt );
            
            public bool Send( byte[] data, bool shouldEncrypt = true )
                => Client.Send( data, shouldEncrypt );
        }

        public override void InitWithMessageHandler( MessageHandlerBase messageHandler )
        {
            Init( _client, messageHandler.WithConnector( this ) );
        }

        public override void OnConnectionAccepted( List<ChannelInfo> welcomeAvailableChannels )
        {
            base.OnConnectionAccepted( welcomeAvailableChannels );
            _heartbeatTimer = new Timer(30000);
            _heartbeatTimer.AutoReset = true;
            _heartbeatTimer.Elapsed += ( sender, args ) => Send( new HeartbeatMessage() );
            _heartbeatTimer.Start();
        }
        
        public override void Connect(string backendEndpoint, int backendPort)
        {
            Logger.Log($"FiveMinuteChat: Connecting to TCP endpoint {backendEndpoint} on port {backendPort}");
            _connectionInitId = Guid.NewGuid();
            _backendEndpoint = backendEndpoint;
            _backendPort = backendPort;
            _client.Connect(backendEndpoint, backendPort);
            _isReconnecting = false;
        }

        public override void SetUsername( string username )
        {
            if( string.IsNullOrWhiteSpace( username ) )
            {
                throw new ArgumentException( $"FiveMinuteChat: Username '{username}' is invalid. It cannot be empty.");
            }

            MessageHandler.SetUsername( username );
            if( _client.Connected )
            {
                Send( new ClientSetUsernameRequest{ Username = username } );
            }
        }
        
        public override void Send(ClientMessageBase message, bool shouldEncrypt = true )
        {
            MainThreadContext.Post( _ => SendOnMainThread(message,shouldEncrypt), null);
        }

        private async void SendOnMainThread( ClientMessageBase message, bool shouldEncrypt = true )
        {
            try
            {
                var connectionInitIdOnSend = _connectionInitId;
                var retries = 0;
                if( (!Connected || !CanSendMessages) &&
                    message is HeartbeatMessage )
                {
                    return;
                }

                while( (!Connected || !CanSendMessages) &&
                       Application.isPlaying &&
                       message is not (ClientEncryptedSymmetricKey or ClientCredentialsResponse
                           or ClientUserInfoResponse) )
                {
                    if( connectionInitIdOnSend != _connectionInitId &&
                        message is ClientWhisperHistoryRequest or ClientWhisperHistoryPagedRequest
                            or ClientChannelHistoryRequest or ClientChannelHistoryPagedRequest or HeartbeatMessage )
                    {
                        Logger.LogWarning(
                            $"FiveMinuteChat: Connection resetting, will discard {message.GetType().Name}" );
                        return;
                    }

                    Logger.Log(
                        $"FiveMinuteChat: Waiting to send {message.GetType().Name}, connection is not yet ready to accept messages" );
                    if( retries++ == 5 )
                    {
                        Logger.LogWarning( "FiveMinuteChat: Connection failed!\nReconnecting..." );
                        Reconnect();
                    }
                    else
                    {
                        await AsyncHelper.Delay( 2500 );
                    }
                }

                if( !_client.Send( Serializer.Serialize( message ), shouldEncrypt ) )
                {
                    Logger.LogWarning( "FiveMinuteChat: Disconnected from server!\nReconnecting..." );
                    Reconnect();
                }

                if( message.IsAckRequested )
                {
                    AddToAckQueue( message );
                }
            }
            catch( ArgumentException e )
            {
                Logger.LogError($"FiveMinuteChat: Attempt to send message that failed validation: {e.Message}");
            }
            catch (Exception e)
            {
                Logger.LogError($"FiveMinuteChat: When sending {message.GetType().Name}, caught {e.GetType().Name}: {e.Message}\nReconnecting...");
                Reconnect();
            }
        }
        
        public override void Disconnect( bool allowReconnect )
        {
            CanSendMessages = false;
            _connectionInitId = Guid.NewGuid();
            _shouldReconnect &= allowReconnect;
            _heartbeatTimer?.Dispose();
            _client?.Disconnect();
        }

        public override async void Reconnect()
        {
            if( _isReconnecting )
            {
                return;
            }
            _isReconnecting = true;
            Disconnect( true );
            await AsyncHelper.Delay( 2500 );
            Connect( _backendEndpoint, _backendPort );
        }

        private void OnApplicationQuit()
        {
            _heartbeatTimer?.Dispose();
            _shouldReconnect = false;
        }

        private void OnDisable()
        {
            _heartbeatTimer?.Dispose();
            _shouldReconnect = false;
        }

        private void OnDestroy()
        {
            _heartbeatTimer?.Dispose();
            _shouldReconnect = false;
        }

        private void Update()
        {
            while (_client.Client.GetNextMessage(out var msg))
            {
                switch (msg.eventType)
                {
                    case FiveMinutes.Telepathy.EventType.Connected:
                        Logger.Log("FiveMinuteChat: Connected to server");
                        break;
                    case FiveMinutes.Telepathy.EventType.Data:
                        MessageHandler.Handle(_client, msg);
                        break;
                    case FiveMinutes.Telepathy.EventType.Disconnected:
                        if( _shouldReconnect )
                        {
                            Logger.Log("FiveMinuteChat: Disconnected from server. Reconnecting...");
                            Reconnect();
                        }
                        else
                        {
                            Logger.Log("FiveMinuteChat: Disconnected from server. Shutting down...");
                            _heartbeatTimer?.Dispose();
                        }
                        
                        break;
                }
            }
        }
    }
}
