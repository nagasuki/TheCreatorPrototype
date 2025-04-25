using System;
using System.Text.RegularExpressions;
using FiveMinuteChat.Interfaces;
using FiveMinuteChat.UI;
using FiveMinutes.Model;
using FiveMinutes.Model.Messages.Client;
using FiveMinutes.Model.Enums;
using FiveMinutes.Model.Messages.Server;

namespace FiveMinuteChat
{
    public partial class ChatConnectionBehavior : ConnectionBehaviorBase
    {
        public event OnChannelJoined ChannelJoined;
        public event OnChannelLeft ChannelLeft;
        public event OnChannelInfo ChannelInfoReceived;

        private Regex _commandRegex = new ("/(?:(?:(join|leave|create-channel|whisper|whisper-back|whois|status|channel-info)) ([A-z0-9\\-]+)?(:? (.*))?)|/(whoami|create-channel)");
        private Regex _changeNameRegex = new (@"/nick ([\p{L}\-\d]{3,25}$)");
        private Regex _displayIdRegex = new ("[A-z0-9]{6}[0-9]{4}");

        protected override void InitConnectorWithMessageHandler() 
            => Connector.InitWithMessageHandler( new ChatMessageHandler() );

        protected override void OnStart()
        {
            Connector.ChannelJoined += OnChannelJoined;
            Connector.ChannelLeft += OnChannelLeft;
            Connector.ChannelInfoReceived += OnChannelInfo;

            Subscribe<ServerWhisperMessage>( whisperMessage =>
            {
                if( whisperMessage.IsNew && 
                    whisperMessage.FromUser?.UserType == UserType.Standard )
                {
                    Connector.Send( new ClientWhisperMessageReceivedRequest()
                    {
                        ReceivedMessageId = whisperMessage.MessageId
                    } );
                }
            } );
            
            if( AutoConnect )
            {
                Connect();
            }
        }
        
        private void OnChannelJoined(ChannelInfo channelInfo, bool canLeave, bool isSilenced )
        {
            ChannelJoined?.Invoke(channelInfo, canLeave, isSilenced);
        }
        
        private void OnChannelLeft( string channelName )
        {
            ChannelLeft?.Invoke( channelName );
        }

        private void OnChannelInfo( ChannelInfo channelInfo )
        {
            ChannelInfoReceived?.Invoke( channelInfo);
        }

        public void Send(string message, string parameter)
        {
            if( string.IsNullOrWhiteSpace( message ) )
            {
                Logger.Log( $"FiveMinuteChat: {nameof(ChatConnectionBehavior)}.{nameof(Send)} - Empty messages are not allowed, ignoring..." );
                return;
            }

            var match = _commandRegex.Match(message);
            if(match.Success)
            {
                var switchVal = string.IsNullOrWhiteSpace( match.Groups[1].Value )
                    ? match.Groups[5].Value
                    : match.Groups[1].Value;
                switch (switchVal)
                {
                    case "join":
                        JoinChannel( match.Groups[2].Value );
                        break;
                    case "leave":
                        LeaveChannel( match.Groups[2].Value );
                        break;
                    case "create-channel":
                        CreateChannel( match.Groups[2].Value );
                        break;
                    case "whisper":
                        Whisper( match.Groups[2].Value, match.Groups[3].Value );
                        break;
                    case "whisper-back":
                        if(Guid.TryParse(match.Groups[2].Value, out var messageId))
                        {
                            WhisperInResponseTo( messageId, match.Groups[3].Value );
                        }
                        else
                        {
                            Logger.LogWarning( $"FiveMinuteChat: Bad message id '{match.Groups[2].Value}' for whisper response" );
                        }

                        break;
                    case "nick":
                        return;
                    case "whois":
                        Whois( match.Groups[2].Value );
                        break;
                    case "whoami":
                        Whois( null );
                        break;
                    case "channel-info":
                        GetChannelInfo(match.Groups[2].Value);
                        break;
                    case "status":
                        SetCustomStatus($"{match.Groups[2].Value} {match.Groups[3].Value}" );
                        break;
                    default:
                        Logger.LogWarning($"FiveMinuteChat: Unknown command {match.Groups[1].Value}");
                        return;
                }
            }
            else if( (match = _changeNameRegex.Match( message ) ).Success )
            {
                SetUsername( match.Groups[1].Value );
            }
            else
            {
                SendChatMessage(parameter, message);
            }
        }
    }
}
