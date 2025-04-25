using System;
using FiveMinutes.Model.Messages.Client;
using UnityEngine;
using ClientChannelInfoRequest = FiveMinutes.Model.Messages.Client.ClientChannelInfoRequest;
using ClientChatMessage = FiveMinutes.Model.Messages.Client.ClientChatMessage;
using ClientGenerateChannelRequest = FiveMinutes.Model.Messages.Client.ClientGenerateChannelRequest;
using ClientGenerateNamedChannelRequest = FiveMinutes.Model.Messages.Client.ClientGenerateNamedChannelRequest;
using ClientJoinChannelRequest = FiveMinutes.Model.Messages.Client.ClientJoinChannelRequest;
using ClientLeaveChannelRequest = FiveMinutes.Model.Messages.Client.ClientLeaveChannelRequest;
using ClientReportChatMessageRequest = FiveMinutes.Model.Messages.Client.ClientReportChatMessageRequest;
using ClientUserInfoRequest = FiveMinutes.Model.Messages.Client.ClientUserInfoRequest;
using ClientWhisperMessage = FiveMinutes.Model.Messages.Client.ClientWhisperMessage;

namespace FiveMinuteChat
{
    public partial class ChatConnectionBehavior
    {
        public void SetUsername( string username ) => Connector?.SetUsername( username );
        
        public void CreateChannel( string channelName )
        {
            var message = string.IsNullOrEmpty( channelName )
                ? new ClientGenerateChannelRequest()
                : new ClientGenerateNamedChannelRequest
                {
                    ChannelName = channelName
                };
            Connector.Send( message );
        }
        
        public void JoinChannel( string channelName )
        {
            Connector.Send( new ClientJoinChannelRequest()
            {
                ChannelName = channelName
            } );
        }
        
        public void LeaveChannel( string channelName )
        {
            Connector.Send( new ClientLeaveChannelRequest()
            {
                ChannelName = channelName
            } );
        }
        
        public void Whisper( string recipientDisplayId, string message )
        {
            if( string.IsNullOrWhiteSpace(recipientDisplayId) )
            {
                Logger.LogError($"FiveMinuteChat: {recipientDisplayId} cannot be empty");
                return;
            }
            if( !_displayIdRegex.IsMatch( recipientDisplayId ) )
            {
                Logger.Log($"FiveMinuteChat: {recipientDisplayId} is not a valid user display id, assuming it's a uniqueUserId or group name");
            }
            Connector.Send( new ClientWhisperMessage
            {
                Recipient = recipientDisplayId,
                Content = message
            } );
            // Left as an exercise for the developer:
            // Display the sent whisper message in the chat log directly on send 
        }
        
        public void WhisperInResponseTo( Guid messageId, string message )
        {
            Connector.Send( new ClientWhisperMessage
            {
                Recipient = $"respondTo:{messageId}",
                Content = message
            } );
        }

        public void Whois( string userDisplayId )
        {
            Connector.Send( new ClientUserInfoRequest
            {
                UserDisplayId =  userDisplayId
            } );
        }

        public void GetChannelInfo( string channelName )
        {
            Connector.Send( new ClientChannelInfoRequest()
            {
                ChannelName = channelName
            } );
        }

        public void SendChatMessage( string channelName, string message )
        {
            Connector.Send( new ClientChatMessage
            {
                Content = message,
                ChannelName = channelName
            } );
        }

        public void SendMessageReport( Guid messageId, string message )
        {
            Connector.Send( new ClientReportChatMessageRequest()
            {
                ReportDescription = message,
                ReportedMessageId = messageId
            } );
        }

        public void SetCustomStatus( string status )
        {
            Connector.Send( new ClientSetMetadataRequest()
            {
                Metadata = new()
                {
                    new()
                    {
                        Key = "CustomStatus",
                        Value = status
                    }
                }
            } );
        }
    }
}
