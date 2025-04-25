using System.Linq;
using FiveMinuteChat.Helpers;
using FiveMinuteChat.Interfaces;
using FiveMinutes.Model;
using FiveMinutes.Model.Enums;
using FiveMinutes.Model.Messages.Client;
using FiveMinutes.Model.Messages.Server;

namespace FiveMinuteChat.UI
{
    public class ChatMessageHandler : MessageHandlerBase
    {
        protected override void RegisterHandlers()
        {
            Handlers.Add( typeof(ServerChatMessage), ( _, _ ) => { } );
            Handlers.Add( typeof(ServerJoinChannelResponse), (client, message) => OnServerJoinChannelResponse(client, message as ServerJoinChannelResponse) );
            Handlers.Add( typeof(ServerLeaveChannelResponse), (client, message) => OnServerLeaveChannelResponse(client, message as ServerLeaveChannelResponse) );
            Handlers.Add( typeof(ServerGenerateChannelResponse), (client, message) => OnServerGenerateChannelResponse(client, message as ServerGenerateChannelResponse) );
            Handlers.Add( typeof(ServerGenerateNamedChannelResponse), (client, message) => OnServerGenerateNamedChannelResponse(client, message as ServerGenerateNamedChannelResponse) );
            Handlers.Add( typeof(ServerChannelHistoryResponse), (client, message) => OnServerChannelHistoryResponse(client, message as ServerChannelHistoryResponse) );
            Handlers.Add( typeof(ServerChannelHistoryPagedResponse), (client, message) => OnServerChannelHistoryResponse(client, message as ServerChannelHistoryPagedResponse) );
            Handlers.Add( typeof(ServerChannelInfoResponse), (client, message) => OnServerChannelInfoResponse(client, message as ServerChannelInfoResponse) );
            Handlers.Add( typeof(ServerUserSilenceInfoMessage), ( _, _ ) => { } );
            Handlers.Add( typeof(ServerUserBanInfoMessage), (client, message) => OnServerUserBanInfoMessage(client, message as ServerUserBanInfoMessage) );
            Handlers.Add( typeof(ServerUserKickInfoMessage), (client, message) => OnServerUserKickInfoMessage(client, message as ServerUserKickInfoMessage) );
            Handlers.Add( typeof(ServerWhisperMessage), ( _, _ ) => { } );
            Handlers.Add( typeof(ServerWhisperHistoryResponse), ( client, message ) => OnServerWhisperHistoryResponseMessage( client, message as ServerWhisperHistoryResponse) );
            Handlers.Add( typeof(ServerWhisperHistoryPagedResponse), ( client, message ) => OnServerWhisperHistoryResponseMessage( client, message as ServerWhisperHistoryPagedResponse) );
        }
        
        protected override void OnServerWelcome( IConnectorClient client, ServerWelcome welcome )
        {
            Logger.Log("FiveMinuteChat: Server accepted connection!");
            if (welcome.AvailableChannels.Any())
            {
                var alreadyJoinedChannels = welcome.AvailableChannels
                    .Where(c => c.IsMember)
                    .ToList();
                var defaultChannels = welcome.AvailableChannels
                    .Where(c => c.IsDefault)
                    .ToList();
                if (alreadyJoinedChannels.Any())
                {
                    Logger.Log($"FiveMinuteChat: Previously joined channels {alreadyJoinedChannels.Select(c => c.Name).Aggregate( (f,s) => $"{f}, {s}" )}");
                    foreach (var alreadyJoinedChannel in alreadyJoinedChannels)
                    {
                        Connector.OnChannelJoined( alreadyJoinedChannel, !alreadyJoinedChannel.IsDefault, alreadyJoinedChannel.IsSilenced );
                        var historyRequest = new ClientChannelHistoryRequest
                        {
                            ChannelName = alreadyJoinedChannel.Name,
                            MaxMessages = 25,
                            IsAckRequested = false
                        };
                        Connector.Send( historyRequest );
                    }
                }
                else if( defaultChannels.Any() ) // assume defaultChannels is a subset of alreadyJoinedChannels
                {
                    Logger.Log($"FiveMinuteChat: Joining default channel(s) {defaultChannels.Select(c => c.Name).Aggregate( (f,s) => $"{f}, {s}" )}");
                    foreach (var defaultChannel in defaultChannels)
                    {
                        var joinChannelRequest = new ClientJoinChannelRequest
                        {
                            ChannelName = defaultChannel.Name,
                        };
                        Connector.Send(joinChannelRequest);
                    }
                }

                var remainingChannels = welcome.AvailableChannels
                    .Except(alreadyJoinedChannels)
                    .Except(defaultChannels)
                    .ToList();
                if (remainingChannels.Any())
                {
                    Logger.Log($"FiveMinuteChat: Remaining available channels are {remainingChannels.Select(c => c.Name).Aggregate( (f,s) => $"{f}, {s}" )}");
                }

                var whisperHistoryRequest = new ClientWhisperHistoryPagedRequest
                {
                    UniqueUserId = UniqueUserId,
                    NewMessagesOnly = false,
                    IsAckRequested = false,
                    MessageDirection = MessageDirection.All
                };
                Connector.Send( whisperHistoryRequest );
                
                Connector.OnConnectionAccepted(welcome.AvailableChannels);
            }
            else
            {
                Logger.LogWarning("FiveMinuteChat: There are no available channels! Disconnecting...");
                Connector.Disconnect( false );
            }
        }
        
        private void OnServerJoinChannelResponse( IConnectorClient _, ServerJoinChannelResponse joinChannelResponse )
        {
            if (joinChannelResponse.Success)
            {
                Logger.Log($"FiveMinuteChat: Successfully joined channel: {joinChannelResponse.ChannelInfo.Name}");
                Connector.OnChannelJoined( joinChannelResponse.ChannelInfo, true, joinChannelResponse.IsSilenced );
                var historyRequest = new ClientChannelHistoryRequest
                {
                    ChannelName = joinChannelResponse.ChannelInfo.Name,
                    MaxMessages = 25,
                    IsAckRequested = false
                };
                Connector.Send( historyRequest );
            }
            else
            {
                Logger.Log($"FiveMinuteChat: Failed to join channel {joinChannelResponse.ChannelInfo.Name}: {joinChannelResponse.Reason}");
            }
        }
        
        private void OnServerLeaveChannelResponse( IConnectorClient _, ServerLeaveChannelResponse leaveChannelResponse )
        {
            if (leaveChannelResponse.Success)
            {
                Logger.Log($"FiveMinuteChat: Successfully left channel: {leaveChannelResponse.ChannelName}");
                Connector.OnChannelLeft( leaveChannelResponse.ChannelName);
            }
            else
            {
                Logger.Log($"FiveMinuteChat: Failed to leave channel: {leaveChannelResponse.ChannelName}");
            }
        }

        private void OnServerGenerateNamedChannelResponse( IConnectorClient _, ServerGenerateNamedChannelResponse response )
        {
            if( response.Success )
            {
                Logger.Log($"FiveMinuteChat: Successfully created channel: {response.ChannelInfo.Name}");
            }
            else
            {
                Logger.Log($"FiveMinuteChat: Failed to create channel: {response.FailureReason}");
            }
        }

        private void OnServerGenerateChannelResponse( IConnectorClient _, ServerGenerateChannelResponse response )
        {
            if( response.Success )
            {
                Logger.Log($"FiveMinuteChat: Successfully created channel: {response.ChannelInfo.Name}");
            }
            else
            {
                Logger.Log($"FiveMinuteChat: Failed to create channel: {response.FailureReason}");
            }
        }
        
        private async void OnServerChannelHistoryResponse( IConnectorClient _, ServerChannelHistoryResponse historyResponse )
        {
            Logger.Log($"FiveMinuteChat: Got channel history for: {historyResponse.ChannelName} ({historyResponse.ChatMessages.Count} messages)");
            foreach (var cm in historyResponse.ChatMessages.OrderBy( cm => cm.SentAt))
            {
                Connector.On( cm );
                await AsyncHelper.Delay( 25 );
            }
        }

        private void OnServerChannelInfoResponse( IConnectorClient _, ServerChannelInfoResponse response )
        {
            if( !response.Success )
            {
                Logger.Log($"FiveMinuteChat: Got channel info response with failure: {response.FailureReason}");
            }
            else
            {
                Logger.Log($"FiveMinuteChat: Got channel info for: {response.ChannelName} - {response.Users.Count}");
                Connector.OnChannelInfoReceived( new ChannelInfo
                {
                    Members = response.Users,
                    Name = response.ChannelName
                });
            }
        }

        private void OnServerUserBanInfoMessage( IConnectorClient _, ServerUserBanInfoMessage message )
        {
            if( !string.IsNullOrEmpty( message.ChannelName ) )
            {
                Logger.Log($"FiveMinuteChat: User was kick-banned from: {message.ChannelName}");
                Connector.OnChannelLeft( message.ChannelName );
            }
            else
            {
                Logger.Log($"FiveMinuteChat: User was kick-banned from the server. Shutting down...");
                Connector.Disconnect( false );
            }
        }

        private void OnServerUserKickInfoMessage( IConnectorClient _, ServerUserKickInfoMessage message )
        {
            if( !string.IsNullOrEmpty( message.ChannelName ) )
            {
                Logger.Log($"FiveMinuteChat: User was kick-banned from: {message.ChannelName}");
                Connector.OnChannelLeft( message.ChannelName );
            }
        }
        
        private void OnServerWhisperHistoryResponseMessage( IConnectorClient _, ServerWhisperHistoryResponse response )
        {
            Logger.Log($"FiveMinuteChat: Got {response.WhisperMessages.Count} whispers from history response." );
            foreach( var message in response.WhisperMessages.OrderBy( w => w.SentAt ) )
            {
                // pass them on
                if( string.IsNullOrEmpty( message.ToUser?.Name ) )
                {
                    message.ToUser = new UserInfo
                    {
                        Name = Username
                    };
                }
                Connector.On( message );
            }
        }
    }
}
