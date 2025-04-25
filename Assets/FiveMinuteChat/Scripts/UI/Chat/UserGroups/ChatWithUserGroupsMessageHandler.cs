using System.Linq;
using FiveMinuteChat.Interfaces;
using FiveMinutes.Model.Messages.Client;
using FiveMinutes.Model.Messages.Server;

namespace FiveMinuteChat.UI
{
    public class ChatWithUserGroupsMessageHandler : ChatMessageHandler
    {
        private readonly ChatConnectionWithUserGroupsBehavior _connectionBehavior;

        public ChatWithUserGroupsMessageHandler( ChatConnectionWithUserGroupsBehavior connectionBehavior )
        {
            _connectionBehavior = connectionBehavior;
        }

        protected override void RegisterHandlers()
        {
            base.RegisterHandlers();
            Handlers.Add( typeof(ServerAddUsersToPersonalGroupResponse), ( _, _ ) => { } );
        }

        protected override void OnServerWelcome( IConnectorClient client, ServerWelcome welcome )
        {
            base.OnServerWelcome( client, welcome );

            var otherUserIds = _connectionBehavior
                .transform
                .parent
                .GetComponentsInChildren<ChatConnectionWithUserGroupsListenerOnlyBehavior>()
                .Select( c => c.UserId )
                .ToList();

            if( otherUserIds.Any() )
            {
                _connectionBehavior.SendRegisterGroup( "friends", otherUserIds );
            }
            
            foreach( var channel in welcome.AvailableChannels )
            {
                if( channel.IsMember )
                {
                    client.Send( new ClientChannelInfoRequest
                    {
                        ChannelName = channel.Name,
                        IsAckRequested = false
                    } );
                }
            }
        }
    }
}
