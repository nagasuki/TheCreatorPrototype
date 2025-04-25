using System;
using System.Runtime.InteropServices;
using FiveMinuteChat.Helpers;
using FiveMinuteChat.Interfaces;
using FiveMinuteChat.UI;
using FiveMinutes.Helpers;
using FiveMinutes.Model.CustomTypes;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using FiveMinutes.Model.Messages;
using FiveMinutes.Model.Messages.Client;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
namespace FiveMinuteChat.Connectors
{
    public class SignalRCoreConnector : ConnectorBase
    {
        private readonly WebGLSignalRClientContainer _signalRClient = new ();

        private class WebGLSignalRClientContainer : IConnectorClient
        {
            public bool Connected { get; private set; }
            public MessageHandlerBase MessageHandler { get; set; }


            [DllImport( "__Internal" )]
            private static extern void ConnectExt( string backendEndpoint, int port, string listedMethod );

            [DllImport( "__Internal" )]
            private static extern void SendExt( string message );

            [DllImport( "__Internal" )]
            private static extern void StopExt();
            
            public async void Connect( string backendEndpoint, int port )
                => ConnectExt( backendEndpoint, port, $"GenericEncodedBinary{MessageBase.SupportedApiVersion.AsSignalRMethodSuffix}" );

            public async void ReConnect()
                => throw new NotImplementedException();

            public async void Disconnect()
            {
                Connected = false;
                StopExt();
            }

            public bool Send( MessageBase message, bool shouldEncrypt = true )
            {
                var serializedMessage = Serializer.Serialize( message );
                //Logger.Log($"FiveMinuteChat: Sending message of type {message.GetType()}: '{serializedMessage}' - " + $"(base64: '{Convert.ToBase64String(serializedMessage)}')");
                SendExt( Convert.ToBase64String(serializedMessage) );
                return true;
            }

            public bool Send( byte[] data, bool shouldEncrypt = true ) 
                => throw new NotImplementedException();

            public void OnConnected( string state )
            {
                Connected = true;
            }

            public void OnSignalRMessage( string serializedMessage ) => 
                MessageHandler.Handle(this, Serializer.Deserialize( Convert.FromBase64String( serializedMessage ) ) );
        }

        public override void InitWithMessageHandler( MessageHandlerBase messageHandler )
        {
            _signalRClient.MessageHandler = messageHandler;
            Init( _signalRClient, messageHandler.WithConnector(this));
            
            
            var listener = FindObjectOfType<WebGLCallbackListener>();
            listener.Subscribe<string>("GenericEncodedBinary", _signalRClient.OnSignalRMessage );
            listener.Subscribe<string>("Connected", _signalRClient.OnConnected );
        }

        public override void Connect( string backendEndpoint, int port )
            => _signalRClient.Connect( backendEndpoint, port );

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

        public override bool Connected => _signalRClient.Connected;  
        public override async void Reconnect()
        {
            CanSendMessages = false;
            _signalRClient.ReConnect();
        }

        public override async void Disconnect( bool allowReconnect )
        {
            CanSendMessages = false;
            if( _signalRClient?.Connected ?? false )
            {
                _signalRClient.Disconnect();
            }
            if( allowReconnect )
            {
                Reconnect();
            }
        }
    }
}

#endif
