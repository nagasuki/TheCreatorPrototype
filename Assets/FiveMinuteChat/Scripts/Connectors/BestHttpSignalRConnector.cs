#if FiveMinuteChat_BestHttpEnabled
using System;
using FiveMinuteChat.Helpers;
using FiveMinuteChat.Interfaces;
using FiveMinutes.Model.Messages;
using FiveMinutes.Model.Messages.Client;
using BestHTTP.SignalRCore;
using BestHTTP.SignalRCore.Encoders;
using FiveMinutes.Helpers;

namespace FiveMinuteChat.Connectors
{
    public class BestHttpSignalRConnector : ConnectorBase
    {
        private readonly SignalRClientContainer _signalRClient = new ();

        private class SignalRClientContainer : IConnectorClient
        {
            public HubConnection Hub { get; set; }
            
            public bool Connected => Hub.State == ConnectionStates.Connected;
            public MessageHandlerBase MessageHandler { get; set; }

            public async void Connect( string backendEndpoint, int backendPort )
            {
                Hub = new HubConnection(new Uri($"{backendEndpoint}:{backendPort}/signalr"), new JsonProtocol( new LitJsonEncoder()) );

                Hub.On<string>( $"GenericEncodedBinary{MessageBase.SupportedApiVersion.AsSignalRMethodSuffix}", OnSignalRMessage );
                await Hub.ConnectAsync();
            }

            public async void Disconnect()
                => await Hub.CloseAsync();

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
                _signalRClient.Hub.SendAsync( "GenericContainerized", new MessageContainer(message) );
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

        public override bool Connected => _signalRClient.Hub.State == ConnectionStates.Connected;  
        public override async void Reconnect()
        {
            await _signalRClient.Hub.ConnectAsync();
        }

        public override async void Disconnect( bool allowReconnect )
        {
            await _signalRClient.Hub.CloseAsync();
            if( allowReconnect )
            {
                Reconnect();
            }
        }
    }
}
#endif
