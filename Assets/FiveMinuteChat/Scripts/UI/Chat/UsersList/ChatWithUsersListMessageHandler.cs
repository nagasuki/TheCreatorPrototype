using FiveMinuteChat.Interfaces;
using FiveMinutes.Model.Messages.Client;
using FiveMinutes.Model.Messages.Server;

namespace FiveMinuteChat.UI
{
    public class ChatWithUsersListMessageHandler : ChatMessageHandler
    {
        protected override void OnServerWelcome( IConnectorClient client, ServerWelcome welcome )
        {
            base.OnServerWelcome( client, welcome );
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
