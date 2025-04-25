using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using FiveMinuteChat.Interfaces;
using FiveMinutes.Extensions;
using FiveMinutes.Helpers;
using FiveMinutes.Model;
using FiveMinutes.Model.Enums;
using FiveMinutes.Model.Messages;
using FiveMinutes.Model.Messages.Client;
using FiveMinutes.Model.Messages.Server;
using FiveMinutes.Telepathy;
using UnityEngine;

namespace FiveMinuteChat.Helpers
{
    public abstract class MessageHandlerBase
    {
        protected SynchronizationContext MainThreadContext;
        protected IConnector Connector;
        protected readonly Dictionary<Type, Action<IConnectorClient, MessageBase>> Handlers = new();
        
        private string _applicationName;
        private string _applicationSecret;
        protected string UniqueUserId { get; private set; }
        protected string Username { get; private set; }

        public MessageHandlerBase WithConnector( IConnector connector )
        {
            MainThreadContext = SynchronizationContext.Current;
            
            Connector = connector;

            Handlers.Add( typeof(ServerHello), (client, message) => OnServerHello(client, message as ServerHello) );
            Handlers.Add( typeof(ServerAck), (client, message) => OnServerAck(client, message as ServerAck) );
            Handlers.Add( typeof(ServerCredentialsRequest), OnServerCredentialsRequest );
            Handlers.Add( typeof(ServerUserInfoRequest), OnServerUserInfoRequest );
            Handlers.Add( typeof(ServerUserInfoResponse), (client, message) => OnServerUserInfoResponse(client, message as ServerUserInfoResponse) );
            Handlers.Add( typeof(ServerWelcome), (client, message) => OnServerWelcome(client, message as ServerWelcome) );
            Handlers.Add( typeof(ServerSetUsernameResponse), (client, message) => OnServerSetUsernameResponse(client, message as ServerSetUsernameResponse) );
            Handlers.Add( typeof(ServerGoodbye), (client, message) => OnServerGoodbye(client, message as ServerGoodbye) );
            RegisterHandlers();
            
            return this;
        }

        protected virtual void OnServerWelcome( IConnectorClient client, ServerWelcome message )
        {
            Connector.OnConnectionAccepted( message.AvailableChannels );
        }

        protected abstract void RegisterHandlers();
        
        public void Handle(IConnectorClient client, Message message)
        {
            MessageBase deserializedMessage;
            try
            {
                deserializedMessage = Serializer.Deserialize( message.data );
            }
            catch( Exception )
            {
                Logger.LogError( $"FiveMinuteChat: Data Message from {message.connectionId} could not be deserialized. Payload is '{Encoding.UTF8.GetString(message.data)}'" );
                return;
            }

            Handle( client, deserializedMessage );
        }
        
        public void Handle(IConnectorClient client, MessageBase message)
        {
            var messageType = message.GetType();
            try
            {
                Connector.On( message );
                if (Handlers.TryGetValue(messageType, out var handler))
                {
                    MainThreadContext.Post( _ => handler.Invoke(client, message), null );
                }
                else
                {
                    Logger.Log($"FiveMinuteChat: Message of type {messageType} has no associated handler.");
                }
            }
            catch( Exception e )
            {
                Logger.LogError($"FiveMinuteChat: Unhandled exception for message of type {messageType.Name}: {e.InnerMostMessage()}\n{e.StackTrace}");
            }
        }

        public void SetCredentials( string applicationName, string applicationSecret, string uniqueUserId )
        {
            Logger.Log($"FiveMinuteChat: Set credentials appName={applicationName}, secret={applicationSecret}, userid={uniqueUserId}");
            _applicationName = applicationName;
            _applicationSecret = applicationSecret;
            UniqueUserId = uniqueUserId;
        }
        
        public void SetUsername( string username )
        {
            Username = username;
        }
        
        private void OnServerSetUsernameResponse( IConnectorClient _, ServerSetUsernameResponse setUsernameResponse )
        {
            if( setUsernameResponse.Success )
            {
                Logger.Log($"FiveMinuteChat: Username successfully changed to {setUsernameResponse.Username}");
            }
            else
            {
                Logger.LogWarning($"FiveMinuteChat: Username change failed: {setUsernameResponse.Reason}");
            }
        }

        private void OnServerHello( IConnectorClient _, ServerHello hello )
        {
            Logger.Log("FiveMinuteChat: Initializing transport encryption...");
            var response = new ClientEncryptedSymmetricKey
            {
                EncryptedSymmetricKey = CryptoHelper.Client.EncryptAsymmetric(CryptoHelper.Client.GetSymmetricKey(), hello.PublicKeyXml),
                EncryptedSymmetricIV = CryptoHelper.Client.EncryptAsymmetric(CryptoHelper.Client.GetSymmetricIV(), hello.PublicKeyXml),
                IsAckRequested = false
            };

            Connector.Send(response, false );
        }

        private void OnServerAck( IConnectorClient _, ServerAck message )
        {
            Connector.AckMessage(message.MessageId);
        }

        private void OnServerCredentialsRequest( IConnectorClient client, MessageBase request )
        {
            Logger.Log("FiveMinuteChat: Encrypted channel established. Supplying server with credentials....");
            var response = new ClientCredentialsResponse
            {
                ApplicationId = _applicationName,
                ApplicationSecret = _applicationSecret,
                IsAckRequested = false
            };
            Connector.Send(response);
        }
        
        private void OnServerUserInfoRequest( IConnectorClient client, MessageBase message )
        {
            Logger.Log("FiveMinuteChat: Supplying server with user info....");
            MainThreadContext.Post( _ =>
            {
                var response = new ClientUserInfoResponse
                {
                    UniqueUserId = UniqueUserId,
                    Username = string.IsNullOrWhiteSpace(Username) ? string.Empty : Username,
                    IsAckRequested = false,
                    RuntimePlatform = Application.platform.ToString(),
                    RuntimeVersion = Application.unityVersion,
                    RuntimeMode = Application.installMode.ToString(),
                    SystemLanguage = Application.systemLanguage.ToString()
                };
                Connector.Send(response);
            }, null );
        }
        
        private void OnServerUserInfoResponse( IConnectorClient _, ServerUserInfoResponse message )
        {
            Logger.Log($"FiveMinuteChat: User info response received: {(message.Success ? "success" : "failure")} {message.Username} ({message.UserDisplayId})");
            // fake whisper
            var wm = message.Success 
                ? new ServerWhisperMessage
                {
                    Content = $"= User Info =\nUsername: {message.Username}\nDisplayId: {message.UserDisplayId}",
                    FromUser = new UserInfo
                    {
                        Name = "SYSTEM",
                        DisplayId = "SYSTEM",
                        UserType = UserType.System
                    },
                    SentAt = DateTime.UtcNow,
                    ToUser = new UserInfo
                    {
                        Name = message.Username!,
                        DisplayId = message.UserDisplayId!
                    }
                } 
                : new ServerWhisperMessage
                {
                    Content = message.Reason!,
                    FromUser = new UserInfo
                    {
                        Name = "SYSTEM",
                        DisplayId = "SYSTEM",
                        UserType = UserType.System
                    },
                    SentAt = DateTime.UtcNow,
                    ToUser = new UserInfo
                    {
                        Name = message.Username!,
                        DisplayId = message.UserDisplayId!
                    }
                };
            Connector.On( wm );
        }
        
        private void OnServerGoodbye( IConnectorClient _, ServerGoodbye goodbye )
        {
            Logger.LogWarning($"FiveMinuteChat: Server closing connection: {goodbye.Reason}");
            if( goodbye.AllowAutoReconnect && 
                Application.isPlaying )
            {
                Connector.Reconnect();
            }
            else
            {
                Connector.Disconnect( false );
            }
        }
    }
}
