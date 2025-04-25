using System;
using FiveMinuteChat.Helpers;
using FiveMinuteChat.Interfaces;
using FiveMinuteChat.UI;
using FiveMinutes.Helpers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using FiveMinutes.Model.Messages;
using FiveMinutes.Model.Messages.Client;

#if !UNITY_WEBGL || UNITY_EDITOR
namespace FiveMinuteChat.Connectors
{
    public class SignalRCoreConnector : ConnectorBase
    {
        private readonly SignalRClientContainer _signalRClient = new ();

        private class SignalRClientContainer : IConnectorClient
        {
            public HubConnection Hub { get; set; }
            
            public bool Connected => Hub?.State == HubConnectionState.Connected;
            public MessageHandlerBase MessageHandler { get; set; }

            public async void Connect( string backendEndpoint, int backendPort )
            {
                Logger.Log($"FiveMinuteChat: Connecting to endpoint {backendEndpoint}:{backendPort}/signalr");
                Hub = new HubConnectionBuilder()
                    .WithUrl( $"{backendEndpoint}:{backendPort}/signalr" )
                    .WithAutomaticReconnect()
                    .AddJsonProtocol()
                    .Build();
                
                Hub.On<string>( $"GenericEncodedBinary{MessageBase.SupportedApiVersion.AsSignalRMethodSuffix}", OnSignalRMessage );
                await Hub.StartAsync();
            }

            public async void Disconnect()
                => await Hub.StopAsync();

            public bool Send( MessageBase message, bool shouldEncrypt = true )
            {
                Hub.SendAsync( "GenericEncodedBinary", Convert.ToBase64String( Serializer.Serialize( message ) ) );
                return true;
            }

            public bool Send( byte[] data, bool shouldEncrypt = true ) 
                => throw new NotImplementedException();

            private void OnSignalRMessage( string serializedMessage )
                => MessageHandler.Handle(this, Serializer.Deserialize( Convert.FromBase64String( serializedMessage ) ) );
        }
        
        public override void InitWithMessageHandler( MessageHandlerBase messageHandler )
        {
            _signalRClient.MessageHandler = messageHandler;
            Init( _signalRClient, messageHandler.WithConnector( this ) );
        }

        public override void Connect( string backendEndpoint, int backendPort )
            => _signalRClient.Connect( backendEndpoint, backendPort );

        public override void Send( ClientMessageBase message, bool shouldEncrypt = true )
        {
            try
            {
                if( message.IsAckRequested )
                {
                    AddToAckQueue( message );
                }

                _signalRClient.Send( message );
            }
            catch( ArgumentException e )
            {
                Logger.LogError($"FiveMinuteChat: Attempt to send message that failed validation: {e.Message}");
            }
            catch( Exception e )
            {
                Logger.LogError($"FiveMinuteChat: Caught exception: {e.Message}\nReconnecting...");
                throw;
            }
        }

        public override bool Connected => _signalRClient.Hub.State == HubConnectionState.Connected;  
        public override async void Reconnect()
        {
            await _signalRClient.Hub.StartAsync();
        }

        public override async void Disconnect( bool allowReconnect )
        {
            if( _signalRClient?.Connected ?? false )
            {
                await _signalRClient.Hub.StopAsync();
            }
            if( allowReconnect )
            {
                Reconnect();
            }
        }
    }
}
#endif
